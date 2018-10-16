using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

/// <summary>
/// A library that allows for easy manipulation of the registry
/// </summary>
public static class EasyRegistry
{
    /// <summary>
    /// Checks if the specified registry key exists
    /// </summary>
    /// <param name="keyName">The full registry path of the key</param>
    /// <returns>true if keyName exists. false if not</returns>
    public static bool KeyExists(string keyName)
    {
        if (Registry.GetValue(keyName, "testval", 1) == null)
            return false;
        else
            return true;
    }

    /// <summary>
    /// Checks if the specified name/value pair exists within the specified registry key
    /// </summary>
    /// <param name="keyName">The full registry path of the key</param>
    /// <param name="valueName">The name of the pair</param>
    /// <returns>true if valueName exists within keyName. false if valueName doesn't exist within keyName or keyName doesn't exist</returns>
    public static bool ValueExists(string keyName, string valueName)
    {
        if (KeyExists(keyName))
            if (Registry.GetValue(keyName, valueName, null) == null)
                return false;
            else
                return true;
        else
            return false;
    }

    /// <summary>
    /// Gets the specified name/value pair from the specified registry key
    /// </summary>
    /// <param name="keyName">The full registry path of the key</param>
    /// <param name="valueName">The name of the pair</param>
    /// <param name="valueType">The type of value to get from the registry</param>
    /// <param name="returnAsNull">If the value aquired from the registry is returnAsNull, return null</param>
    /// <returns>If the value of the name/value pair is equal to returnAsNull, return null
    /// Otherwise, return the value</returns>
    /// <exception cref="System.IO.IOException">keyName has been marked for deletion</exception>
    /// <exception cref="ArgumentException">keyName doesn't begin with a valid registry root
    /// or keyName doesn't exist
    /// or valueName doesn't exist within keyName
    /// or returnAsNull's type isn't valueType</exception>
    public static object GetValue(string keyName, string valueName, Type valueType, object returnAsNull = null)
    {
        if (returnAsNull.GetType() != valueType)
            throw new ArgumentException("returnAsNull's type isn't valueType");

        if (valueType == typeof(bool))
            return GetValueBool(keyName, valueName);
        else if (valueType == typeof(int))
            return GetValueInt(keyName, valueName);
        else
            return GetValueObject(keyName, valueName, returnAsNull);
    }

    /// <summary>
    /// Deletes the specified registry key
    /// </summary>
    /// <param name="keyName">The full registry path of the key</param>
    /// <exception cref="ArgumentException">keyName doesn't exist
    /// or the root key in keyName isn't one of the supported root keys
    /// or keyName is just the root key</exception>
    /// <exception cref="ArgumentNullException">keyName is null</exception>
    /// <exception cref="System.Security.SecurityException">The user doesn't have permission to delete the value</exception>
    /// <exception cref="ObjectDisposedException">keyName is closed</exception>
    /// <exception cref="UnauthorizedAccessException">keyName is read only</exception>
    public static void DeleteKey(string keyName)
    {
        string[] key_split = keyName.Split('\\');

        #region Check for errors
        if (keyName == null)
            throw new ArgumentNullException("keyName is null");

        for (int i = 1; i < key_split.Length; i++)
        {
            if (key_split[i] != "")
                goto Continue;
        }
        throw new ArgumentException("keyName is just a root key");
        Continue:

        if (!KeyExists(keyName))
            throw new ArgumentException("keyName doesn't exist");
        #endregion

        RegistryKey key;

        #region Get root key
        switch (key_split[0])
        {
            case "HKEY_CLASSES_ROOT":
                key = Registry.ClassesRoot;
                break;
            case "HKEY_LOCAL_MACHINE":
                key = Registry.LocalMachine;
                break;
            case "HKEY_USERS":
                key = Registry.Users;
                break;
            case "HKEY_CURRENT_CONFIG":
                key = Registry.CurrentConfig;
                break;
            case "HKEY_CURRENT_USER":
                key = Registry.CurrentUser;
                break;
            default:
                throw new ArgumentException("The root in keyName isn't one of the supported root keys");
        }
        #endregion

        #region Stitch key_split
        for (int i = 2; i < key_split.Length; i++)
        {
            key_split[1] += '\\' + key_split[i];
        }
        #endregion

        key.DeleteSubKey(key_split[1]);
    }

    /// <summary>
    /// Deletes the specified name/value pair from the specified registry key
    /// </summary>
    /// <param name="keyName">The full registry path of the key</param>
    /// <param name="valueName">The name of the pair</param>
    /// <exception cref="ArgumentException">keyName doesn't exist
    /// or valueName doesn't exist within keyName
    /// or the root key in keyName isn't one of the supported root keys</exception>
    /// <exception cref="ArgumentNullException">keyName is null or valueName is null</exception>
    /// <exception cref="System.Security.SecurityException">The user doesn't have permission to delete the value</exception>
    /// <exception cref="ObjectDisposedException">keyName is closed</exception>
    /// <exception cref="UnauthorizedAccessException">keyName is read only</exception>
    public static void DeleteValue(string keyName, string valueName)
    {
        #region Check for errors
        if (keyName == null)
            throw new ArgumentNullException("keyName is null");
        if (valueName == null)
            throw new ArgumentNullException("valueName is null");
        if (!KeyExists(keyName))
            throw new ArgumentException("keyName doesn't exist");
        if (!ValueExists(keyName, valueName))
            throw new ArgumentException("valueName doesn't exist within keyName");
        #endregion

        RegistryKey key;

        #region Get root key
        string[] key_split = keyName.Split('\\');
        switch (key_split[0])
        {
            case "HKEY_CLASSES_ROOT":
                key = Registry.ClassesRoot;
                break;
            case "HKEY_LOCAL_MACHINE":
                key = Registry.LocalMachine;
                break;
            case "HKEY_USERS":
                key = Registry.Users;
                break;
            case "HKEY_CURRENT_CONFIG":
                key = Registry.CurrentConfig;
                break;
            case "HKEY_CURRENT_USER":
                key = Registry.CurrentUser;
                break;
            default:
                throw new ArgumentException("The root key isn't one of the supported root keys");
        }
        #endregion

        #region Stitch key_split
        if (key_split.Length == 1)
            key_split = new string[] { key_split[0], "" };
        else
        {
            for (int i = 2; i < key_split.Length; i++)
            {
                key_split[1] += '\\' + key_split[i];
            }
        }
        #endregion

        key = key.OpenSubKey(key_split[1], true);
        key.DeleteValue(valueName);
    }

    #region object
    /// <summary>
    /// Gets the specified name/value pair from the specified registry key
    /// </summary>
    /// <param name="keyName">The full registry path of the key</param>
    /// <param name="valueName">The name of the pair</param>
    /// <param name="returnAsNull">If the value aquired from the registry is returnAsNull, return null</param>
    /// <returns>If the value of the name/value pair is equal to returnAsNull, return null
    /// Otherwise, return the value</returns>
    /// <exception cref="System.Security.SecurityException">The user doesn't have permission to read from keyName</exception>
    /// <exception cref="System.IO.IOException">keyName has been marked for deletion</exception>
    /// <exception cref="ArgumentException">keyName doesn't begin with a valid registry root</exception>
    public static object GetValueObject(string keyName, string valueName, object returnAsNull = null)
    {
        if (!KeyExists(keyName))
            throw new ArgumentException("keyName doesn't exist");
        else if (!ValueExists(keyName, valueName))
            throw new ArgumentException("valueName doesn't exist within keyName");
        else
        {
            object result = Registry.GetValue(keyName, valueName, null);
            if (result == returnAsNull)
                return null;
            else
                return result;
        }
    }

    /// <summary>
    /// Sets the specified name/value pair on the specified registry key.
    /// Creates the registry key if it doesn't already exist
    /// </summary>
    /// <param name="keyName">The full registry path of the key</param>
    /// <param name="valueName">The name of the pair</param>
    /// <param name="value">The value to store</param>
    /// <param name="valueKind">The type to store the data as</param>
    /// <exception cref="ArgumentNullException">value is null</exception>
    /// <exception cref="ArgumentException">keyName doesn't begin with a valid registry root or keyName is longer than 255 characters</exception>
    /// <exception cref="UnauthorizedAccessException">keyname is a read-only key</exception>
    /// <exception cref="System.Security.SecurityException">The user doesn't have permission to write to the registry</exception>
    public static void SetValue(string keyName, string valueName, object value, RegistryValueKind valueKind)
    {
        Registry.SetValue(keyName, valueName, value, valueKind);
    }
    #endregion

    #region bool
    /// <summary>
    /// Gets the specified name/value pair from the specified registry key
    /// </summary>
    /// <param name="keyName">The full registry path of the key</param>
    /// <param name="valueName">The name of the pair</param>
    /// <returns>The value of the name/value pair</returns>
    /// <exception cref="System.Security.SecurityException">The user doesn't have permission to read from keyName</exception>
    /// <exception cref="System.IO.IOException">keyName has been marked for deletion</exception>
    /// <exception cref="ArgumentException">keyName doesn't begin with a valid registry root</exception>
    public static bool GetValueBool(string keyName, string valueName)
    {
        if (!KeyExists(keyName))
            throw new ArgumentException("keyName doesn't exist");
        else if (!ValueExists(keyName, valueName))
            throw new ArgumentException("valueName doesn't exist within keyName");
        else
        {
            return (int)Registry.GetValue(keyName, valueName, 0) == 1 ? true : false;
        }
    }

    /// <summary>
    /// Sets the specified name/value pair on the specified registry key.
    /// Creates the registry key if it doesn't already exist
    /// </summary>
    /// <param name="keyName">The full registry path of the key</param>
    /// <param name="valueName">The name of the pair</param>
    /// <param name="value">The value to store</param>
    /// <exception cref="ArgumentNullException">value is null</exception>
    /// <exception cref="ArgumentException">keyName doesn't begin with a valid registry root or keyName is longer than 255 characters</exception>
    /// <exception cref="UnauthorizedAccessException">keyname is a read-only key</exception>
    /// <exception cref="System.Security.SecurityException">The user doesn't have permission to write to the registry</exception>
    public static void SetValue(string keyName, string valueName, bool value)
    {
        SetValue(keyName, valueName, (value ? 1 : 0), RegistryValueKind.DWord);
    }
    #endregion

    #region int
    /// <summary>
    /// Gets the specified name/value pair from the specified registry key
    /// </summary>
    /// <param name="keyName">The full registry path of the key</param>
    /// <param name="valueName">The name of the pair</param>
    /// <returns>The value of the name/value pair</returns>
    /// <exception cref="System.Security.SecurityException">The user doesn't have permission to read from keyName</exception>
    /// <exception cref="System.IO.IOException">keyName has been marked for deletion</exception>
    /// <exception cref="ArgumentException">keyName doesn't begin with a valid registry root</exception>
    public static int GetValueInt(string keyName, string valueName)
    {
        if (!KeyExists(keyName))
            throw new ArgumentException("keyName doesn't exist");
        else if (!ValueExists(keyName, valueName))
            throw new ArgumentException("valueName doesn't exist within keyName");
        else
        {
            return (int)Registry.GetValue(keyName, valueName, 0);
        }
    }

    /// <summary>
    /// Sets the specified name/value pair on the specified registry key.
    /// Creates the registry key if it doesn't already exist
    /// </summary>
    /// <param name="keyName">The full registry path of the key</param>
    /// <param name="valueName">The name of the pair</param>
    /// <param name="value">The value to store</param>
    /// <exception cref="ArgumentNullException">value is null</exception>
    /// <exception cref="ArgumentException">keyName doesn't begin with a valid registry root or keyName is longer than 255 characters</exception>
    /// <exception cref="UnauthorizedAccessException">keyname is a read-only key</exception>
    /// <exception cref="System.Security.SecurityException">The user doesn't have permission to write to the registry</exception>
    public static void SetValue(string keyName, string valueName, int value)
    {
        SetValue(keyName, valueName, value, RegistryValueKind.DWord);
    }
    #endregion

    #region string
    /// <summary>
    /// Gets the specified name/value pair from the specified registry key
    /// </summary>
    /// <param name="keyName">The full registry path of the key</param>
    /// <param name="valueName">The name of the pair</param>
    /// <param name="returnAsNull">If the value aquired from the registry is returnAsNull, return null</param>
    /// <returns>If the value of the name/value pair is equal to returnAsNull, return null
    /// Otherwise, return the value</returns>
    /// <exception cref="System.Security.SecurityException">The user doesn't have permission to read from keyName</exception>
    /// <exception cref="System.IO.IOException">keyName has been marked for deletion</exception>
    /// <exception cref="ArgumentException">keyName doesn't begin with a valid registry root</exception>
    public static string GetValueString(string keyName, string valueName, string returnAsNull = null)
    {
        if (!KeyExists(keyName))
            throw new ArgumentException("keyName doesn't exist");
        else if (!ValueExists(keyName, valueName))
            throw new ArgumentException("valueName doesn't exist within keyName");
        else
        {
            string result = (string)Registry.GetValue(keyName, valueName, null);
            if (result == returnAsNull)
                return null;
            else
                return result;
        }
    }

    /// <summary>
    /// Sets the specified name/value pair on the specified registry key.
    /// Creates the registry key if it doesn't already exist
    /// </summary>
    /// <param name="keyName">The full registry path of the key</param>
    /// <param name="valueName">The name of the pair</param>
    /// <param name="value">The value to store</param>
    /// <param name="expandString">If value contains unexpanded environment variables, set this to true to expand them when reading from this pair</param>
    /// <exception cref="ArgumentNullException">value is null</exception>
    /// <exception cref="ArgumentException">keyName doesn't begin with a valid registry root or keyName is longer than 255 characters</exception>
    /// <exception cref="UnauthorizedAccessException">keyname is a read-only key</exception>
    /// <exception cref="System.Security.SecurityException">The user doesn't have permission to write to the registry</exception>
    /// <exception cref="ArgumentException">keyName doesn't begin with a valid registry root</exception>
    public static void SetValue(string keyName, string valueName, string value, bool expandString = false)
    {
        Registry.SetValue(keyName, valueName, value, (expandString ? RegistryValueKind.ExpandString : RegistryValueKind.String));
    }

    /// <summary>
    /// Sets the specified name/value pair on the specified registry key.
    /// Creates the registry key if it doesn't already exist
    /// </summary>
    /// <param name="keyName">The full registry path of the key</param>
    /// <param name="valueName">The name of the pair</param>
    /// <param name="value">The value to store</param>
    /// <exception cref="ArgumentNullException">value is null</exception>
    /// <exception cref="ArgumentException">keyName doesn't begin with a valid registry root or keyName is longer than 255 characters</exception>
    /// <exception cref="UnauthorizedAccessException">keyname is a read-only key</exception>
    /// <exception cref="System.Security.SecurityException">The user doesn't have permission to write to the registry</exception>
    /// <exception cref="ArgumentException">keyName doesn't begin with a valid registry root</exception>
    public static void SetValue(string keyName, string valueName, string value)
    {
        SetValue(keyName, valueName, value, false);
    }
    #endregion

    #region string[]
    /// <summary>
    /// Gets the specified name/value pair from the specified registry key
    /// </summary>
    /// <param name="keyName">The full registry path of the key</param>
    /// <param name="valueName">The name of the pair</param>
    /// <param name="returnAsNull">If the value aquired from the registry is returnAsNull, return null</param>
    /// <returns>If the value of the name/value pair is equal to returnAsNull, return null
    /// Otherwise, return the value</returns>
    /// <exception cref="System.Security.SecurityException">The user doesn't have permission to read from keyName</exception>
    /// <exception cref="System.IO.IOException">keyName has been marked for deletion</exception>
    /// <exception cref="ArgumentException">keyName doesn't begin with a valid registry root</exception>
    public static string[] GetValueStringArray(string keyName, string valueName, string[] returnAsNull)
    {
        if (!KeyExists(keyName))
            throw new ArgumentException("keyName doesn't exist");
        else if (!ValueExists(keyName, valueName))
            throw new ArgumentException("valueName doesn't exist within keyName");
        else
        {
            string[] result = (string[])Registry.GetValue(keyName, valueName, null);
            if (result == returnAsNull)
                return null;
            else
                return result;
        }        
    }

    /// <summary>
    /// Sets the specified name/value pair on the specified registry key.
    /// Creates the registry key if it doesn't already exist
    /// </summary>
    /// <param name="keyName">The full registry path of the key</param>
    /// <param name="valueName">The name of the pair</param>
    /// <param name="value">The value to store</param>
    /// <exception cref="ArgumentNullException">value is null</exception>
    /// <exception cref="ArgumentException">keyName doesn't begin with a valid registry root or keyName is longer than 255 characters</exception>
    /// <exception cref="UnauthorizedAccessException">keyname is a read-only key</exception>
    /// <exception cref="System.Security.SecurityException">The user doesn't have permission to write to the registry</exception>
    /// <exception cref="ArgumentException">keyName doesn't begin with a valid registry root</exception>
    public static void SetValue(string keyName, string valueName, string[] value)
    {
        Registry.SetValue(keyName, valueName, value, RegistryValueKind.MultiString);
    }
    #endregion
}