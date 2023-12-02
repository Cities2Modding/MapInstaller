using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MapInstaller
{
    /// <summary>
    /// Automatically searches BepInEx directory and copies all changed maps
    /// </summary>
    internal class Installer
    {
        static char _S = Path.DirectorySeparatorChar;
        static string GAME_PATH = Path.GetDirectoryName( UnityEngine.Application.dataPath );
        static string BEPINEX_PATH = Path.Combine( GAME_PATH, $"BepInEx{_S}plugins" );
        static string MAPS_PATH = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), $"AppData{_S}LocalLow{_S}Colossal Order{_S}Cities Skylines II{_S}Maps" );
        static List<Action> _currentActions = new List<Action>( );

        private static ManualLogSource _logger;
        private static bool _hasErrors = false;

        internal Installer( ManualLogSource logger )
        {
            _logger = logger;
        }

        /// <summary>
        /// Scan all BepInEx plugin folders for maps directories
        /// </summary>
        private void ScanDirectory( )
        {
            _logger.LogInfo( "Scanning BepInEx folder..." );

            try
            {
                foreach ( var directory in Directory.GetDirectories( BEPINEX_PATH ) )
                {
                    ProcessDirectory( directory );
                }

                foreach ( var zipFile in Directory.GetFiles( BEPINEX_PATH, "*.zip", SearchOption.AllDirectories ) )
                {
                    ProcessZipFile( zipFile );
                }

                // If no actions were queued there's no changes
                if ( _currentActions.Count == 0 )
                {
                    OnComplete( );
                    _logger.LogInfo( "No changes detected!" );
                }
            }
            catch ( Exception ex )
            {
                HandleException( ex );
            }
        }

        /// <summary>
        /// Ensure the local maps folder exists
        /// </summary>
        /// <remarks>
        /// (No need to do an exists check as this does nothing if
        /// it already exists.)
        /// </remarks>
        private void EnsureMapsFolder( )
        {
            try
            {
                Directory.CreateDirectory( MAPS_PATH );
            }
            catch ( Exception ex )
            {
                HandleException( ex );
            }
        }

        /// <summary>
        /// Search directories for maps
        /// </summary>
        /// <param name="directory"></param>
        private void ProcessDirectory( string directory )
        {
            var mapsFolder = Path.Combine( directory, "Maps" );

            // If it has a maps folder we should do a copy!
            if ( Directory.Exists( mapsFolder ) && FolderHasChanges( mapsFolder, MAPS_PATH ) )
            {
                var readableFolderString = Path.GetRelativePath( BEPINEX_PATH, mapsFolder );
                _logger.LogInfo( $"Detected changes at '{readableFolderString}', queuing for copy..." );
                _currentActions.Add( GenerateDirectoryCopyTask( mapsFolder ) );
            }
        }

        /// <summary>
        /// Search a ZIP file for any maps
        /// </summary>
        /// <param name="zipFilePath"></param>
        private void ProcessZipFile( string zipFilePath )
        {
            var readableString = Path.GetRelativePath( BEPINEX_PATH, zipFilePath );

            if ( ZipFileHasChanges( zipFilePath, MAPS_PATH ) )
            {
                _logger.LogInfo( $"Detected changes in map ZIP '{readableString}', queuing for copy..." );
                _currentActions.Add( GenerateZipCopyTask( zipFilePath ) );
            }
        }

        /// <summary>
        /// Copy an individual file to the target directory
        /// </summary>
        /// <param name="file"></param>
        /// <param name="targetFolder"></param>
        private void CopyFile( string file, string targetFolder )
        {
            try
            {
                var fileName = Path.GetFileName( file );
                var targetPath = Path.Combine( targetFolder, fileName );
                File.Copy( file, targetPath, true );
            }
            catch ( Exception ex )
            {
                HandleException( ex );
            }
        }

        /// <summary>
        /// Check if any file in the source folder has changed compared to the target folder.
        /// </summary>
        /// <param name="sourceFolder"></param>
        /// <param name="targetFolder"></param>
        /// <returns></returns>
        private bool FolderHasChanges( string sourceFolder, string targetFolder )
        {
            var sourceFiles = Directory.GetFiles( sourceFolder, "*.*" );

            foreach ( var sourceFile in sourceFiles )
            {
                var fileName = Path.GetFileName( sourceFile );
                var targetFile = Path.Combine( targetFolder, fileName );

                // If the target file doesn't exist
                if ( !File.Exists( targetFile ) )
                    return true;

                // Check if the file has changed
                if ( GetFileHash( sourceFile ) != GetFileHash( targetFile ) )
                    return true;
            }

            return false; // No changes detected
        }

        /// <summary>
        /// Check if a ZIP file has any changes if it has map files
        /// </summary>
        /// <param name="zipFilePath"></param>
        /// <param name="targetFolder"></param>
        /// <returns></returns>
        private bool ZipFileHasChanges( string zipFilePath, string targetFolder )
        {
            using ( var archive = ZipFile.OpenRead( zipFilePath ) )
            {
                foreach ( var entry in archive.Entries )
                {
                    // Skip if the entry is a directory or does not contain the 'Maps' folder in its path
                    if ( entry.FullName.EndsWith( "/" ) || !entry.FullName.ToLowerInvariant( ).Contains( "maps/" ) )
                        continue;

                    // Extract the relative path of the file within the 'Maps' directory in the ZIP archive
                    var targetFilePath = SanitiseZipEntryPath( Path.GetFileName( entry.FullName ), MAPS_PATH );

                    // If the file does not exist in the target folder, it's considered a change
                    if ( !File.Exists( targetFilePath ) )
                        return true;

                    // Check if the file has changed
                    if ( GetZipEntryHash( entry ) != GetFileHash( targetFilePath ) )
                        return true;
                }
            }
            return false; // No changes detected in zip file compared to target folder
        }

        /// <summary>
        /// Gets an MD5 hash for a file
        /// </summary>
        /// <remarks>
        /// (Used to detect changes)
        /// </remarks>
        /// <param name="stream"></param>
        /// <returns></returns>
        private string GetHash( Stream stream )
        {
            using ( var md5 = MD5.Create( ) )
            {
                var hash = md5.ComputeHash( stream );
                return BitConverter.ToString( hash ).Replace( "-", "" ).ToLowerInvariant( );
            }
        }

        /// <summary>
        /// Get a local file system file MD5 hash
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private string GetFileHash( string file )
        {
            using ( var stream = File.OpenRead( file ) )
            {
                return GetHash( stream );
            }
        }

        /// <summary>
        /// Gets a zip entry file MD5 hash
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        private string GetZipEntryHash( ZipArchiveEntry entry )
        {
            using ( var stream = entry.Open( ) )
            {
                return GetHash( stream );
            }
        }

        /// <summary>
        /// Sanitise a ZIP entry path to ensure no dodgey exploits!
        /// </summary>
        /// <param name="entryFullName"></param>
        /// <param name="targetDirectory"></param>
        /// <returns></returns>
        /// <exception cref="SecurityException"></exception>
        private string SanitiseZipEntryPath( string entryFullName, string targetDirectory )
        {
            // Normalize the zip entry path to prevent directory traversal
            var sanitisedPath = Path.GetFullPath( Path.Combine( targetDirectory, entryFullName ) );

            // Ensure the sanitized path still resides within the target directory
            if ( !sanitisedPath.StartsWith( targetDirectory, StringComparison.OrdinalIgnoreCase ) )
                throw new SecurityException( "Attempted to extract a file outside of the target directory." );

            return sanitisedPath;
        }

        /// <summary>
        /// Generate copy tasks for a specific map folder
        /// </summary>
        /// <param name="sourceFolder"></param>
        /// <returns></returns>
        private Action GenerateDirectoryCopyTask( string sourceFolder )
        {
            return ( ) =>
            {
                try
                {
                    var readableFolderString = Path.GetRelativePath( BEPINEX_PATH, sourceFolder );
                    var files = Directory.GetFiles( sourceFolder, "*.*" );

                    if ( files.Length == 0 )
                        return;

                    _logger.LogInfo( $"Copying '{files.Length}' from '{readableFolderString}'." );

                    var progress = 0;
                    var complete = 0;

                    foreach ( var file in files )
                    {
                        if ( progress % 10 == 0 )
                            _logger.LogInfo( $"Copying file {complete}/{files.Length}..." );

                        CopyFile( file, MAPS_PATH );
                        complete++;
                        progress = ( int ) ( ( complete / ( decimal ) files.Length ) * 100 );
                    }

                    _logger.LogInfo( $"Finished copying '{readableFolderString}'." );
                }
                catch ( Exception ex )
                {
                    HandleException( ex );
                }
            };
        }

        /// <summary>
        /// Generate copy tasks for maps located in ZIP files
        /// </summary>
        /// <param name="zipFilePath"></param>
        /// <returns></returns>
        private Action GenerateZipCopyTask( string zipFilePath )
        {
            return ( ) =>
            {
                try
                {
                    var readableString = Path.GetRelativePath( BEPINEX_PATH, zipFilePath );

                    _logger.LogInfo( $"Processing zip file '{readableString}'." );

                    using ( var archive = ZipFile.OpenRead( zipFilePath ) )
                    {
                        // Filter out the relevant entries and count them
                        var relevantEntries = archive.Entries
                            .Where( entry => entry.FullName.ToLower().Contains( "maps/" ) )
                            .ToList( );

                        var totalEntries = relevantEntries.Count;
                        if ( totalEntries == 0 )
                            return;

                        _logger.LogInfo( $"Extracting '{totalEntries}' files from '{readableString}'." );

                        var complete = 0;

                        foreach ( var entry in relevantEntries )
                        {
                            var targetPath = SanitiseZipEntryPath( Path.GetFileName( entry.FullName ), MAPS_PATH );

                            // Ensure the target directory exists
                            Directory.CreateDirectory( Path.GetDirectoryName( targetPath ) );

                            // Extract the file to the target path
                            entry.ExtractToFile( targetPath, true ); // Overwrite if exists

                            complete++;
                            var progress = ( int ) ( ( complete / ( decimal ) totalEntries ) * 100 );

                            if ( progress % 10 == 0 )
                                _logger.LogInfo( $"Extracting file {complete}/{totalEntries}..." );
                        }
                    }

                    _logger.LogInfo( $"Finished processing zip file '{readableString}'." );
                }
                catch ( Exception ex )
                {
                    HandleException( ex );
                }
            };
        }

        /// <summary>
        /// Run all of the copy actions in a serial manner so we don't hammer
        /// HDD/SSD drives.
        /// </summary>
        private void RunActions( )
        {
            if ( _currentActions.Count == 0 )
            {
                OnComplete( );
                return;
            }

            Task.Run( ( ) =>
            {
                foreach ( var action in _currentActions )
                    action( );

                OnComplete( );
            } );
        }

        /// <summary>
        /// Clear the list to let GC clear memory
        /// </summary>
        private void Clear( )
        {
            _currentActions.Clear( );
        }

        /// <summary>
        /// Handle exceptions, use best practice of only handling
        /// exceptions we expect within our context.
        /// </summary>
        /// <param name="ex"></param>
        private void HandleException( Exception ex )
        {
            if ( ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is SecurityException ||
                ex is InvalidDataException /* For handling invalid or corrupt zip files */ ||
                ex is FileNotFoundException /* If a zip file is not found */)
            {
                _logger.LogError( ex );
                _hasErrors = true;
            }
            else
                throw ex; // Rethrow if it's not an expected exception
        }

        /// <summary>
        /// Check for errors and advise the user if necessary
        /// </summary>
        private void CheckForErrors( )
        {
            if ( !_hasErrors )
                return;

            _logger.LogInfo( @"Map installer encountered errors trying to copy maps, " +
                "for support please visit the Cities2Modding discord referencing the error." );
            _logger.LogInfo( @"See BepInEx log file at: " + BEPINEX_PATH );
        }

        /// <summary>
        /// Executed when the installer actions are complete
        /// </summary>
        private void OnComplete( )
        {
            CheckForErrors( );
            Clear( );
        }

        /// <summary>
        /// Run the installer tasks
        /// </summary>
        public void Run( )
        {
            EnsureMapsFolder( );
            ScanDirectory( );
            RunActions( );
        }
    }
}