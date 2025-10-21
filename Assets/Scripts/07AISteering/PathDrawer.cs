using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathDrawer : MonoBehaviour
{
    public Color pathColor = Color.white;
    public List<Transform> pathNodes = new List<Transform>();
    public bool isLooping = false;

    void OnDrawGizmos()
    {
        Gizmos.color = pathColor;

        Transform[] childNodes = GetComponentsInChildren<Transform>();
        pathNodes.Clear();

        foreach (Transform child in childNodes)
        {
            if (child != this.transform)
            {
                pathNodes.Add(child);
            }
        }

        for (int i = 0; i < pathNodes.Count; i++)
        {
            Vector3 currentNodePos = pathNodes[i].position;
            Gizmos.DrawWireSphere(currentNodePos, 0.03f);

            if (i > 0)
            {
                Vector3 prevNodePos = pathNodes[i - 1].position;
                Gizmos.DrawLine(prevNodePos, currentNodePos);
            }
            else if (isLooping && pathNodes.Count > 1)
            {
                Vector3 lastNodePos = pathNodes[pathNodes.Count - 1].position;
                Gizmos.DrawLine(lastNodePos, currentNodePos);
            }
        }
    }
}
