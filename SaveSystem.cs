/*
  Save System - Extention for Unity to use advanced saving in your game.
  Created by Donut Studio, September 12, 2022.
  Released into the public domain.
*/

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using UnityEngine;

namespace DonutStudio.Utilities.Saving
{
    /// <summary>
    /// This class is used to save/load the GameSave class.
    /// </summary>
    public static class SaveSystem
    {
        public static string Path { get; private set; }
        public static string FileName { get; private set; }
        public static SaveMethod Method { get; private set; }
        public static bool IsInitialized { get; private set; } = false;
        public static GameSave GameSave { get; set; } = new GameSave();

        private static byte[] key;

        /// <summary>
        /// Initialize this class with the given parameters to start loading/saving.
        /// </summary>
        /// <param name="_path">The path of the file. Application.persistentDataPath is recommended!</param>
        /// <param name="_fileName">The name of the file.</param>
        /// <param name="_method">The method how you want to save/load the data.</param>
        /// <param name="_key">If you are using AES, enter a key with the length of 16, 24 or 32.</param>
        /// <returns></returns>
        public static bool Initialize(string _path, string _fileName, SaveMethod _method, byte[] _key = null)
        {
            if (IsInitialized)
                return false;
            
            Path = _path;
            FileName = _fileName;
            Method = _method;
            
            if (Method == SaveMethod.aes)
            {
                if (_key == null || (_key.Length != 16 && _key.Length != 24 && _key.Length != 32))
                    return false;
                else
                    key = _key;
            }

            CreateDirectory();
            IsInitialized = true;
            return true;
        }
        /// <summary>
        /// Initialize this class with the given parameters to start loading/saving.
        /// </summary>
        /// <param name="_path">The path of the file. Application.persistentDataPath is recommended!</param>
        /// <param name="_fileName">The name of the file.</param>
        /// <param name="_method">The method how you want to save/load the data.</param>
        /// <param name="password">If you are using AES, enter a string representing the key (will be converted).</param>
        /// <returns></returns>
        public static bool Initialize(string _path, string _fileName, SaveMethod _method, string _password = null)
        {
            return Initialize(_path, _fileName, _method, GetKeyFromString(_password));
        }

        /// <summary>
        /// Create a directory if does not exist.
        /// </summary>
        public static void CreateDirectory()
        {
            if (!Directory.Exists(Path))
                Directory.CreateDirectory(Path);
        }
        /// <summary>
        /// Reset the game by deleting the file and saving it again.
        /// </summary>
        /// <returns></returns>
        public static bool Reset()
        {
            if (!IsInitialized)
                return false;

            if (!Delete())
                return false;
            GameSave = new GameSave();
            Save();
            return true;
        }
        /// <summary>
        /// Delete the save file from the path.
        /// </summary>
        /// <returns></returns>
        public static bool Delete()
        {
            if (!IsInitialized || !File.Exists(GetFullPath()))
                return false;

            File.Delete(GetFullPath());
            return true;
        }

        /// <summary>
        /// Save the game with the given method.
        /// </summary>
        /// <returns></returns>
        public static bool Save()
        {
            if (!IsInitialized)
                return false;

            switch (Method)
            {
                case SaveMethod.binary:
                    return SaveBinary();
                case SaveMethod.json:
                    return SaveJson();
                default:
                    return SaveAES();
            }
        }
        private static bool SaveJson()
        {
            try
            {
                File.WriteAllText(GetFullPath(), JsonUtility.ToJson(GameSave));
            }
            catch (System.Exception)
            {
                return false;
            }
            return true;
        }
        private static bool SaveBinary()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(GetFullPath(), FileMode.OpenOrCreate);

            try
            {
                formatter.Serialize(stream, GameSave);
            }
            catch (System.Exception)
            {
                return false;
            }
            finally
            {
                stream.Close();
            }
            return true;
        }
        private static bool SaveAES()
        {
            Aes aes = Aes.Create();
            aes.Key = key;
            byte[] inputIV = aes.IV;

            FileStream stream = new FileStream(GetFullPath(), FileMode.OpenOrCreate);
            try
            {
                stream.Write(inputIV, 0, inputIV.Length);
            }
            catch (System.Exception)
            {
                stream.Close();
                return false;
            }

            CryptoStream cryptoStream = new CryptoStream(stream, aes.CreateEncryptor(aes.Key, aes.IV), CryptoStreamMode.Write);
            StreamWriter writer = new StreamWriter(cryptoStream);
            string jsonString = JsonUtility.ToJson(GameSave);

            try
            {
                writer.Write(jsonString);
            }
            catch (System.Exception)
            {
                return false;
            }
            finally
            {
                writer.Close();
                cryptoStream.Close();
                stream.Close();
            }
            
            return true;
        }

        /// <summary>
        /// Load the game with the given method.
        /// </summary>
        /// <returns></returns>
        public static bool Load()
        {
            if (!IsInitialized || !File.Exists(GetFullPath()))
                return false;

            switch (Method)
            {
                case SaveMethod.binary:
                    return LoadBinary();
                case SaveMethod.json:
                    return LoadJson();
                default:
                    return LoadAES();
            }
        }
        private static bool LoadJson()
        {
            try
            {
                GameSave = JsonUtility.FromJson<GameSave>(File.ReadAllText(GetFullPath()));
            }
            catch (System.Exception)
            {
                return false;
            }
            return true;
        }
        private static bool LoadBinary()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(GetFullPath(), FileMode.Open);

            try
            {
                GameSave = formatter.Deserialize(stream) as GameSave;
            }
            catch (System.Exception)
            {
                return false;
            }
            finally
            {
                stream.Close();
            }

            return true;
        }
        private static bool LoadAES()
        {
            FileStream stream = new FileStream(GetFullPath(), FileMode.Open);
            Aes aes = Aes.Create();
            byte[] outputIV = new byte[aes.IV.Length];
            try
            {
                stream.Read(outputIV, 0, outputIV.Length);
            }
            catch (System.Exception)
            {
                stream.Close();
                return false;
            }
            
            CryptoStream cryptoStream = new CryptoStream(stream, aes.CreateDecryptor(key, outputIV), CryptoStreamMode.Read);
            StreamReader reader = new StreamReader(cryptoStream);

            try
            {
                string text = reader.ReadToEnd();
                GameSave = JsonUtility.FromJson<GameSave>(text);
            }
            catch(System.Exception)
            {
                stream.Close();
                return false;
            }

            reader.Close();
            cryptoStream.Close();
            stream.Close();
            return true;
        }

        /// <summary>
        /// Get the full save path.
        /// </summary>
        /// <returns></returns>
        public static string GetFullPath()
        {
            return Path + '/' + FileName;
        }
        /// <summary>
        /// Get a key for the aes algorithm with a length of 16, 24 or 32 according to your password string.
        /// </summary>
        /// <param name="password">The string to convert the key from.</param>
        /// <returns></returns>
        public static byte[] GetKeyFromString(string password)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(password);

            if (bytes.Length != 16 && bytes.Length != 24 && bytes.Length != 32)
            {
                byte[] key;

                if (bytes.Length < 16)
                    key = new byte[16];
                else if (bytes.Length < 24)
                    key = new byte[24];
                else
                    key = new byte[32];

                for (int i = 0; i < key.Length; i++)
                    key[i] = bytes[i % bytes.Length];
                return key;
            }
            else
                return bytes;
        }
    }

    /// <summary>
    /// The available methods for saving the data.
    /// </summary>
    public enum SaveMethod
    {
        binary,
        json,
        aes
    }
}