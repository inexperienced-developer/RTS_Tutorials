using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectionManager : MonoBehaviour
{
    [SerializeField] private LayerMask m_selectableLayer;
    [SerializeField] private GameObject m_selectionCanvasPrefab;
    [SerializeField] private Color m_selectionBoxColor = new Color(0, 1, 0, 0.3f);
    private static RectTransform m_selectionBox;
    private static Image m_selectionBoxImg;

    public static event Action<List<ISelectable>> s_SelectionChanged;
    private List<ISelectable> m_selectedUnits = new List<ISelectable>();

    private Vector2 m_startPos, m_endPos;

    private void Awake()
    {
        if(m_selectionBox == null)
        {
            GameObject canvas = Instantiate(m_selectionCanvasPrefab, Vector3.zero, Quaternion.identity);
            canvas.name = "SelectionCanvas";
            m_selectionBoxImg = canvas.GetComponentInChildren<Image>(true);
            m_selectionBox = m_selectionBoxImg.gameObject.GetComponent<RectTransform>();
            m_selectionBoxImg.color = m_selectionBoxColor;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            m_startPos = Input.mousePosition;
            ClearSelection();
        }
        if (Input.GetMouseButton(0))
        {
            m_endPos = Input.mousePosition;
            //Draw the selection box
            DrawSelection();
        }
        if (Input.GetMouseButtonUp(0))
        {
            //Select within the box
            SelectUnits();
        }
    }

    private void ClearSelection()
    {
        foreach(var unit in m_selectedUnits)
        {
            unit.OnDeselect();
        }
        m_selectedUnits.Clear();
        s_SelectionChanged?.Invoke(m_selectedUnits);
    }

    private void DrawSelection()
    {
        //Turn box on if not on already
        if(!m_selectionBox.gameObject.activeSelf)
            m_selectionBox.gameObject.SetActive(true);
        //Get the width and the height
        float width = m_endPos.x - m_startPos.x;
        float height = m_endPos.y - m_startPos.y;
        //Set the rectTransform size
        m_selectionBox.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));
        //Find the center
        Vector3 center = m_startPos + new Vector2(width / 2, height / 2);
        //Set the position of the box to the center
        m_selectionBox.position = center;
    }

    private void SelectUnits()
    {
        //Turn off selection box
        m_selectionBox.gameObject.SetActive(false);
        //Get the positions of our corners
        //Raycast from our corners to the map
        Vector3[] corners = SelectionCornersWS();
        //Find the center of those points
        Vector3 center = Vector3.zero;
        foreach (var c in corners)
            center += c;
        center /= 4;
        //Orient the points to a standard so we can calculate all permutations of the box being drawn
        Vector3[] oriented = OrientPoints(corners);
        //Get the size of the box in world space
        Vector3 widthVector = (oriented[2] - oriented[1] + oriented[3] - oriented[0]) / 2; //(TR-TL + BR-BL) / 2
        Vector3 heightVector = (oriented[1] - oriented[0] + oriented[2] - oriented[3]) / 2; //(TL-BL + TR-BR) / 2
        Vector3 size = new Vector3(widthVector.magnitude, heightVector.magnitude, 1);
        //Calculate the rotation in world space
        Quaternion rot = CalculateRotation(oriented);
        //Perform an OverlapBox for all objects on selectable layer
        Collider[] cols = Physics.OverlapBox(center, size / 2, rot, m_selectableLayer);
        //Select all selectables
        foreach(var col in cols)
        {
            ISelectable selectable = col.GetComponent<ISelectable>();
            if (selectable == null) continue;
            selectable.OnSelect();
            m_selectedUnits.Add(selectable);
        }
        s_SelectionChanged?.Invoke(m_selectedUnits);
    }

    private Vector3[] SelectionCornersWS()
    {
        //startPos, point2, point3, and endPos
        //Get the other 2 corners
        Vector3 point2 = new Vector2(m_startPos.x, m_endPos.y);
        Vector3 point3 = new Vector2(m_endPos.x, m_startPos.y);
        Vector3[] cornersSS = new Vector3[4] { m_startPos, point2, point3, m_endPos };
        //Create a hit array to keep track
        RaycastHit[] hits = new RaycastHit[4];
        Vector3[] cornersWS = new Vector3[4];
        for (int i = 0; i < 4; i++)
        { 
            //We assume we always hit a collider
            //Can be managed with an invisible trigger that is larger than the play area
            Physics.Raycast(Camera.main.ScreenPointToRay(cornersSS[i]), out hits[i], Mathf.Infinity);
            cornersWS[i] = hits[i].point;
        }
        return cornersWS;
    }

    private Vector3[] OrientPoints(Vector3[] corners)
    {
        // Clockwise organization of points BL->TL->TR->BR
        // Check orientation based on our selection box
        Vector3[] oriented = new Vector3[4];
        if (m_startPos.x < m_endPos.x && m_startPos.y < m_endPos.y)
        {
            oriented[0] = corners[0]; //Bottom Left
            oriented[1] = corners[1]; //Top Left
            oriented[2] = corners[3]; //Top Right
            oriented[3] = corners[2]; //Bottom Right
        }
        else if (m_startPos.x < m_endPos.x && m_startPos.y >= m_endPos.y)
        {
            oriented[0] = corners[1]; //Bottom Left
            oriented[1] = corners[0]; //Top Left
            oriented[2] = corners[2]; //Top Right
            oriented[3] = corners[3]; //Bottom Right
        }
        else if (m_startPos.x >= m_endPos.x && m_startPos.y >= m_endPos.y)
        {
            oriented[0] = corners[3]; //Bottom Left
            oriented[1] = corners[2]; //Top Left
            oriented[2] = corners[0]; //Top Right
            oriented[3] = corners[1]; //Bottom Right
        }
        else
        {
            oriented[0] = corners[2]; //Bottom Left
            oriented[1] = corners[3]; //Top Left
            oriented[2] = corners[1]; //Top Right
            oriented[3] = corners[0]; //Bottom Right
        }
        return oriented;
    }

    private Quaternion CalculateRotation(Vector3[] orientedPoints)
    {
        //Need to calculate the directions of each the x, y, and z axes
        Vector3 x = (orientedPoints[3] - orientedPoints[0]).normalized; // Bottom Right - Bottom Left
        Vector3 y = (orientedPoints[1] - orientedPoints[0]).normalized; // Top Left - Bottom Left
        Vector3 z = Vector3.Cross(x, y); // Z (forward) will be the cross product (perpendicular) of the 2 vectors
        //Matrix multiplication to calculate the rotation
        Matrix4x4 rotMatrix = new Matrix4x4(x, y, z, new Vector4(0, 0, 0, 1));
        return rotMatrix.rotation;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (Input.GetMouseButton(0))
        {
            //Raycast from our corners to the map
            Vector3[] corners = SelectionCornersWS();
            //Find the center of those points
            //Center is Average of points (a + b + c + d) / 4
            Vector3 center = Vector3.zero;
            foreach (var c in corners)
            {
                Gizmos.DrawWireSphere(c, 0.5f);
                center += c;
            }
            center /= 4;
            //Get the oriented points
            Vector3[] oriented = OrientPoints(corners);
            //Calculate Rotation
            Quaternion rot = CalculateRotation(oriented);
            ////Get the size
            Vector3 widthVector = (oriented[2] - oriented[1] + oriented[3] - oriented[0]) / 2; //(TR-TL + BR-BL) / 2
            Vector3 heightVector = (oriented[1] - oriented[0] + oriented[2] - oriented[3]) / 2; //(TL-BL + TR-BR) / 2
            Vector3 size = new Vector3(widthVector.magnitude, heightVector.magnitude, 1);
            //Draw a wire cube - it will be wrong but it's the good first step
            Gizmos.color = Color.blue;
            Gizmos.matrix = Matrix4x4.TRS(center, rot, size);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
}
