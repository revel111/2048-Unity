using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class Block : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private TextMeshPro _text;

    public int Value;
    public Vector2 Pos => transform.position;
    public Node Node;
    public Block MergingBlock;
    public bool Merging;

    public void Init(BlockType blockType)
    {
        Value = blockType.Value;
        _renderer.color = blockType.Color;
        _text.text = blockType.Value.ToString();
    } 

    public void SetBlock(Node node)
    {
        if (Node != null)
            Node.OccupiedBlock = null;

        Node = node;
        Node.OccupiedBlock = this;
    }

    public void MergeBlock(Block mergeBlock)
    {
        MergingBlock = mergeBlock;

        Node.OccupiedBlock = null;
        mergeBlock.Merging = true;
    }

    public bool CanMerge(int value) => Value == value && !Merging && MergingBlock == null;
}