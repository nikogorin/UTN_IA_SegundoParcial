using System;
using TMPro;
using UnityEngine;

public class Node : MonoBehaviour
{
    public static event Action<Node> OnNodeClicked = delegate { };
    public static event Action<Node> OnNodeRightClicked = delegate { };

    [SerializeField] private Renderer nodeRenderer;
    [SerializeField] private GameObject wallGameObject;
    [SerializeField] private TMP_Text _costText;

    public Coordinates Coordinates { get; private set; }

    private bool _blocked;
    public bool Blocked
    {
        get => _blocked; 
        private set 
        { 
            _blocked = value; 
            wallGameObject.SetActive(value);
            if (value)
                ChangeColor(Color.blueViolet);
            else
                ChangeColor(Color.white);
        }
    }

    private int _cost = 1;
    public int Cost
    {
        get => _cost;
        private set
        {
            _cost = Mathf.Clamp(value, 1, 5);
            _costText.text = _cost.ToString();
            _costText.enabled = _cost > 1;
        }
    }

    public void ResetColor()
    {
        ChangeColor(Color.white);
    }

    public void SetupCoordinates(Coordinates coordinates)
    {
        Coordinates = coordinates;
        name = $"Node {coordinates}";
        Cost = 1;
    }

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(0))
            OnNodeClicked?.Invoke(this);
        else if (Input.GetMouseButtonDown(1))
            OnNodeRightClicked?.Invoke(this);
        else if (Input.GetMouseButtonDown(2))
            Blocked = !Blocked;
        else if (Input.GetKeyDown(KeyCode.S))
            Cost = 2;
        else if (Input.GetKeyDown(KeyCode.W))
            Cost = 5;
    }

    public void ChangeColor(Color color) => nodeRenderer.material.color = color;

    private void OnValidate()
    {
        if(nodeRenderer != null)
            return;
        if (!TryGetComponent(out nodeRenderer))
            Debug.LogError("This gameobject doesn't have Renderer component");
    }
}
