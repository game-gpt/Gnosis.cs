using System;
using System.IO;

namespace Gnosis.Core.Platform;

/// <summary>
/// 提供原子性文件写入操作
/// </summary>
public static class AtomicFileOperations
{
    /// <summary>
    /// 以原子操作方式将文本内容写入文件
    /// </summary>
    /// <param name="path">目标文件路径</param>
    /// <param name="content">要写入的文本内容</param>
    public static void WriteAllText(string path, string content)
    {
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = path + ".tmp";

        try
        {
            File.WriteAllText(tempPath, content);
            File.Move(tempPath, path, overwrite: true);
        }
        catch (Exception)
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }
    }

    /// <summary>
    /// 以原子操作方式将字节数组写入文件
    /// </summary>
    /// <param name="path">目标文件路径</param>
    /// <param name="content">要写入的字节数组</param>
    public static void WriteAllBytes(string path, byte[] content)
    {
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = path + ".tmp";

        try
        {
            File.WriteAllBytes(tempPath, content);
            File.Move(tempPath, path, overwrite: true);
        }
        catch (Exception)
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }
    }

    /// <summary>
    /// 以原子操作方式替换目标文件
    /// </summary>
    /// <param name="sourcePath">源文件路径</param>
    /// <param name="destinationPath">目标文件路径</param>
    /// <param name="backupPath">备份文件路径，为空则不创建备份</param>
    public static void SafeReplace(string sourcePath, string destinationPath, string backupPath = "")
    {
        var directory = Path.GetDirectoryName(destinationPath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!string.IsNullOrEmpty(backupPath) && File.Exists(destinationPath))
        {
            var backupDirectory = Path.GetDirectoryName(backupPath);

            if (!string.IsNullOrEmpty(backupDirectory) && !Directory.Exists(backupDirectory))
            {
                Directory.CreateDirectory(backupDirectory);
            }

            File.Copy(destinationPath, backupPath, overwrite: true);
        }

        File.Move(sourcePath, destinationPath, overwrite: true);
    }
}
