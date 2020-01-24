using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utils
{
    //*****************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="objectName"></param>
    /// <returns></returns>
    //*****************************************************************************
    public static List<GameObject> FindObjectsInScene(string objectName)
    {
        UnityEngine.SceneManagement.Scene activeScene =
            UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        GameObject[] rootObjects = activeScene.GetRootGameObjects();

        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        List<GameObject> objectsInScene = new List<GameObject>();

        for (int i = 0; i < rootObjects.Length; i++)
        {
            if (objectName.Equals(rootObjects[i].name))
                objectsInScene.Add(rootObjects[i]);
        }

        for (int i = 0; i < allObjects.Length; i++)
        {
            if (allObjects[i].transform.root)
            {
                for (int i2 = 0; i2 < rootObjects.Length; i2++)
                {
                    if (allObjects[i].transform.root ==
                        rootObjects[i2].transform && allObjects[i] != rootObjects[i2])
                    {
                        if (objectName.Equals(allObjects[i].name))
                            objectsInScene.Add(allObjects[i]);
                        break;
                    }
                }
            }
        }
        return objectsInScene;
    }

    //*****************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="objectName"></param>
    /// <returns></returns>
    //*****************************************************************************
    public static GameObject FindObjectInScene(string objectName)
    {
        var objs = Utils.FindObjectsInScene(objectName);

        if (objs.Count == 0)
            return null;

        return objs[0];
    }

    public static T FindObjectComponentInScene<T>(string objectName)
    {
        var objs = Utils.FindObjectsInScene(objectName);

        if (objs.Count == 0)
            return default;

        return objs[0].GetComponent<T>();
    }

}
