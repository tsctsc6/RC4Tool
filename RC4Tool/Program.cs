using System.CommandLine;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace RC4Tool;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        var fileOption = new Option<FileInfo?>(
           name: "--file",
           description: "The file to encrypt and decrypt.",
           isDefault: false,
           parseArgument: result =>
           {
               string? filePath = result.Tokens.Single().Value;
               if (!File.Exists(filePath))
               {
                   result.ErrorMessage = $"\"{filePath}\" does not exist";
                   return null;
               }
               else
               {
                   return new FileInfo(filePath);
               }
           })
        {
            IsRequired = true
        };

        var passwordOption = new Option<byte[]?>(
           name: "--password",
           description: "The password to encrypt and decrypt file. Up to 256 bits.",
           isDefault: false,
           parseArgument: result =>
           {
               string? password = result.Tokens.Single().Value;
               var passwordBytes = Encoding.UTF8.GetBytes(password);
               if (passwordBytes.Length > 256)
               {
                   result.ErrorMessage = $"\"{password}\" is too long";
                   return null;
               }
               else
               {
                   return passwordBytes;
               }
           })
        {
            IsRequired = true
        };

        var savePasswordOption = new Option<bool>(
            name: "--savePassword",
           description: "The password to encrypt and decrypt file. Up to 256 bits.",
           isDefault: true,
           parseArgument: result =>
           {
               if (!result.Tokens.Any()) return false;
               string? boolString = result.Tokens.Single().Value.ToUpper();
               if (boolString == "TRUE")
               {
                   return true;
               }
               else if (boolString == "FALSE")
               {
                   return false;
               }
               else
               {
                   return false;
               }
           });

        var rootCommand = new RootCommand("RC4 encryption and decryption tool");
        var encryptCommand = new Command("encrypt", "encrypt the file. Generate the .rc4 file in the same location.")
        {
            fileOption,
            passwordOption,
            savePasswordOption
        };
        encryptCommand.SetHandler((filePath, password, isSavePassword) =>
        {
            Encrypt(filePath!, password!, isSavePassword);
        }, fileOption, passwordOption, savePasswordOption);

        var autoEncryptCommand = new Command("autoEncrypt", "auto encrypt the file. Generate the .rc4pak file in the same location.")
        {
            fileOption
        };
        autoEncryptCommand.SetHandler((filePath) =>
        {
            AutoEncrypt(filePath!);
        }, fileOption);

        rootCommand.AddCommand(encryptCommand);
        rootCommand.AddCommand(autoEncryptCommand);
        return await rootCommand.InvokeAsync(args);
    }

    static void Encrypt(FileInfo fileInfo, byte[] password, bool isSavePassword)
    {
        RC4 rc4 = new(password);
        using FileStream fs = File.Open(fileInfo.FullName, FileMode.Open);
        string newFilePath;
        if (fileInfo.Extension == ".rc4")
        {
            newFilePath = fileInfo.FullName[..^4];
            isSavePassword = false;
        }
        else newFilePath = fileInfo.FullName + ".rc4";
        using FileStream fs2 = new(newFilePath, FileMode.Create);
        fs2.WriteBytes(rc4.Encrypt(fs.ReadBytes()));
        if (isSavePassword) File.WriteAllBytes(newFilePath + ".password.txt", password);
    }

    static void AutoEncrypt(FileInfo fileInfo)
    {
        if (fileInfo.Extension == ".rc4pak")
        {
            using ZipArchive archive = ZipFile.OpenRead(fileInfo.FullName);
            var passwordEntry = archive.Entries
                .Single(e => e.FullName == "Password");
            byte[] password = new byte[256];
            using (var passwordStream = passwordEntry.Open())
                passwordStream.Read(password, 0, 256);

            RC4 rc4 = new(password);

            var fileEntry = archive.Entries
                .Single(e => e.FullName == fileInfo.Name[..^3]);
            using var fileStream = fileEntry.Open();
            using FileStream fileStream2 = new(fileInfo.FullName[..^7], FileMode.Create);
            fileStream2.WriteBytes(rc4.Encrypt(fileStream.ReadBytes()));
        }
        else
        {
            using FileStream fs = File.Open(fileInfo.FullName, FileMode.Open);
            string newDirectoryPath = fileInfo.FullName + ".rc4pakd";
            if (Directory.Exists(newDirectoryPath)) Directory.Delete(newDirectoryPath, true);
            Directory.CreateDirectory(newDirectoryPath);
            var password = RandomNumberGenerator.GetBytes(256);
            File.WriteAllBytes(Path.Combine(newDirectoryPath, "Password"), password);
            RC4 rc4 = new(password);
            using (FileStream fs2 = new(
                Path.Combine(newDirectoryPath, fileInfo.Name + ".rc4"), FileMode.Create))
            {
                fs2.WriteBytes(rc4.Encrypt(fs.ReadBytes()));
            }
            string newPakPath = newDirectoryPath[..^1];
            if (File.Exists(newPakPath)) File.Delete(newPakPath);
            ZipFile.CreateFromDirectory(newDirectoryPath, newPakPath, CompressionLevel.NoCompression, false);
            Directory.Delete(newDirectoryPath, true);
        }
    }
}
