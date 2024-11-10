using System.IO;
using Microsoft.AspNetCore.Http;

    public static class FileUtility
    {
        public static bool IsValidFileType(string extension)
        {
            return extension == ".jpg" || extension == ".png" || IsVideoFile(extension);
        }

        public static bool IsVideoFile(string extension)
        {
            return extension == ".mp4" || extension == ".avi" || extension == ".mov" || extension == ".wmv";
        }

        public static async Task<List<string>> SaveFilesAsync(IFormFileCollection files, string destinationFolder)
        {
            var savedFilePaths = new List<string>();

            foreach (var file in files)
            {
                var filePath = Path.Combine(destinationFolder, file.FileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                await using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                savedFilePaths.Add(filePath);
            }

            return savedFilePaths;
        }
    }
