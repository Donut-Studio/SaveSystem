/*
  Save System - Extention for Unity to use advanced saving in your game.
  Created by Donut Studio, September 11, 2022.
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
        /// Before performing any saveing/loading initilaize this class with the given parameters. 
        /// </summary>
        /// <param name="_path">The path of the file.</param>
        /// <param name="_fileName">The name of the file.</param>
        /// <param name="_method">The method how you want to save/load the data.</param>
        /// <param name="_key">If you are using AES enter a key..</param>
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
                if (_key == null)
                    return false;
                else
                    key = _key;
            }

            CreateDirectory();
            IsInitialized = true;
            return true;
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