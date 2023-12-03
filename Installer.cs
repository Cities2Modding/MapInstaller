using BepInEx.Logging;
using Game.Areas;
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
        static string THUNDERSTORE_PATH = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), $"AppData{_S}Roaming{_S}Thunderstore Mod Manager{_S}DataFolder{_S}CitiesSkylines2{_S}profiles" );
        static List<Action> _currentActions = new List<Action>( );

        private static ManualLogSource _logger;
        private static bool _hasErrors = false;
        private static bool _isUsingThunderstore = false;

        internal Installer( ManualLogSource logger )
        {
            _logger = logger;
        }

        /// <summary>
        /// Scan all plugin folders for maps directories
        /// </summary>
        private void ScanDirectory( )
        {

            try
            {
                if ( Directory.Exists( BEPINEX_PATH ) )
                {
                    _logger.LogInfo( "Scanning BepInEx folder..." );
                    ProcessSource( BEPINEX_PATH );
                }

                var thunderStorePath = GetActiveThunderstoreProfile( );

                if ( !string.IsNullOrEmpty( thunderStorePath ) )
                {
                    _logger.LogInfo( $"Scanning Thunderstore folder '{thunderStorePath}'..." );
                    ProcessSource( thunderStorePath );
                    _isUsingThunderstore = true;
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
        /// Get the active Thunderstore profile
        /// </summary>
        /// <returns></returns>
        private string GetActiveThunderstoreProfile( )
        {
            if ( !Directory.Exists( THUNDERSTORE_PATH ) )
                return null;

            DateTime mostRecent = DateTime.MinValue;
            var mostRecentProfile = string.Empty;

            foreach ( var profileDirectory in Directory.GetDirectories( THUNDERSTORE_PATH ) )
            {
                var bepInExPath = Path.Combine( profileDirectory, $"BepInEx{_S}plugins" );

                if ( Directory.Exists( bepInExPath ) )
                {
                    var mostRecentModified = GetMostRecentModifiedDate( bepInExPath );

                    if ( mostRecentModified > mostRecent )
                    {
                        mostRecent = mostRecentModified;
                        mostRecentProfile = bepInExPath;
                    }
                }
            }

            return mostRecentProfile;
        }

        /// <summary>
        /// Gets the most recent date modified date
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public DateTime GetMostRecentModifiedDate( string directory )
        {
            return Directory.GetFiles( directory, "*", SearchOption.AllDirectories )
                                          .Select( file => new FileInfo( file ).LastWriteTime )
                                          .OrderByDescending( date => date )
                                          .FirstOrDefault( );
        }

        /// <summary>
        /// Process a source directory
        /// </summary>
        /// <param name="sourceDirectory"></param>
        private void ProcessSource( string sourceDirectory )
        {
            foreach ( var directory in Directory.GetDirectories( sourceDirectory ) )
            {
                ProcessDirectory( sourceDirectory, directory );
            }

            foreach ( var zipFile in Directory.GetFiles( sourceDirectory, "*.zip", SearchOption.AllDirectories ) )
            {
                ProcessZipFile( sourceDirectory, zipFile );
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
        /// <param name="sourceDirectory"></param>
        /// <param name="directory"></param>
        private void ProcessDirectory( string sourceDirectory, string directory )
        {
            if ( CheckDirectoryForMaps( directory, out var folderToUse ) &&
                FolderHasChanges( folderToUse, MAPS_PATH ) )
            {
                var readableFolderString = Path.GetRelativePath( sourceDirectory, folderToUse );
                _logger.LogInfo( $"Detected changes at '{readableFolderString}', queuing for copy..." );
                _currentActions.Add( GenerateDirectoryCopyTask( sourceDirectory, folderToUse ) );
            }
        }

        /// <summary>
        /// Check a directory for the existence of a map
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="outputFolder"></param>
        /// <returns></returns>
        private bool CheckDirectoryForMaps( string directory, out string outputFolder )
        {
            outputFolder = null;

            // Check for maps directory

            var mapsFolder = Path.Combine( directory, "Maps" );

            if ( Directory.Exists( mapsFolder ) )
            {
                outputFolder = mapsFolder;
                return true;
            }

            // Check for flattened map files
            var mapIDFiles = Directory.GetFiles( directory, "*.cok.cid" );

            if ( mapIDFiles.Length == 0 )
                return false;

            // Check if it has a matching map file
            foreach ( var mapIDFile in mapIDFiles )
            {
                var fileName = Path.GetFileNameWithoutExtension( mapIDFile );
                var mapFile = Path.Combine( Path.GetDirectoryName( mapIDFile ), fileName );

                if ( File.Exists( mapFile ) )
                {
                    outputFolder = directory;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Search a ZIP file for any maps
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <param name="zipFilePath"></param>
        private void ProcessZipFile( string sourceDirectory, string zipFilePath )
        {
            var readableString = Path.GetRelativePath( sourceDirectory, zipFilePath );

            if ( ZipFileHasChanges( zipFilePath, MAPS_PATH ) )
            {
                _logger.LogInfo( $"Detected changes in map ZIP '{readableString}', queuing for copy..." );
                _currentActions.Add( GenerateZipCopyTask( sourceDirectory, zipFilePath ) );
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
            var cokIDFiles = Directory.GetFiles( sourceFolder, "*.cok.cid" );
            var cokFiles = Directory.GetFiles( sourceFolder, "*.cok" );

            if ( cokIDFiles.Length == 0 || cokFiles.Length == 0 )
                return false;

            var allFiles = cokIDFiles.ToList( );
            allFiles.AddRange( cokFiles );

            foreach ( var sourceFile in allFiles )
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
                    if ( entry.FullName.EndsWith( "/" ) || !entry.FullName.ToLowerInvariant( ).Contains( "maps/" )
                        || ( !entry.FullName.ToLowerInvariant( ).EndsWith( ".cok" ) || entry.FullName.ToLowerInvariant( ).EndsWith( ".cok.cid" ) ))
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
        /// <param name="copyFolder"></param>
        /// <returns></returns>
        private Action GenerateDirectoryCopyTask( string sourceFolder, string copyFolder )
        {
            return ( ) =>
            {
                try
                {
                    var readableFolderString = Path.GetRelativePath( sourceFolder, copyFolder );

                    var cokIDFiles = Directory.GetFiles( copyFolder, "*.cok.cid" );
                    var cokFiles = Directory.GetFiles( copyFolder, "*.cok" );

                    if ( cokIDFiles.Length == 0 && cokFiles.Length == 0 )
                        return;

                    var files = cokIDFiles.ToList( );
                    files.AddRange( cokFiles );

                    _logger.LogInfo( $"Copying '{files.Count}' from '{readableFolderString}'." );

                    var progress = 0;
                    var complete = 0;

                    foreach ( var file in files )
                    {
                        if ( progress % 10 == 0 )
                            _logger.LogInfo( $"Copying file {complete + 1}/{files.Count}..." );

                        CopyFile( file, MAPS_PATH );
                        complete++;
                        progress = ( int ) ( ( complete / ( decimal ) files.Count ) * 100 );
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
        /// <param name="sourceDirectory"></param>
        /// <param name="zipFilePath"></param>
        /// <returns></returns>
        private Action GenerateZipCopyTask( string sourceDirectory, string zipFilePath )
        {
            return ( ) =>
            {
                try
                {
                    var readableString = Path.GetRelativePath( sourceDirectory, zipFilePath );

                    _logger.LogInfo( $"Processing zip file '{readableString}'." );

                    using ( var archive = ZipFile.OpenRead( zipFilePath ) )
                    {
                        // Filter out the relevant entries and count them
                        var relevantEntries = archive.Entries
                            .Where( entry => entry.FullName.ToLower().Contains( "maps/" ) 
                            && ( entry.FullName.ToLowerInvariant().EndsWith( ".cok" ) || entry.FullName.ToLowerInvariant( ).EndsWith( ".cok.cid" ) ) )
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
                                _logger.LogInfo( $"Extracting file {complete + 1}/{totalEntries}..." );
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
            _logger.LogInfo( @"See BepInEx log file at: " + ( _isUsingThunderstore ? Path.Combine( GetActiveThunderstoreProfile(), "BepInEx" ) : BEPINEX_PATH ) );
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