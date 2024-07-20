using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;



public class GameManager : MonoBehaviour
{
    [SerializeField] private readonly int _size = 4;
    [SerializeField] private readonly float _travelTime = 0.2f;
    [SerializeField] private Node _nodePrefab;
    [SerializeField] private SpriteRenderer _boardPrefab;
    [SerializeField] private Block _blockPrefab;
    [SerializeField] private List<BlockType> _blockTypes;
    [SerializeField] private int _winCondition = 2048;

    [SerializeField] private GameObject _winScreen, _looseScreen;

    private int _round = 0;
    private List<Node> _nodes = new();
    private List<Block> _blocks = new();
    private GameState _gameState;

    private BlockType GetBlockTypeByValue(int value) => _blockTypes.First(x => x.Value == value);

    public void Start()
    {
        GenerateGrid();
    }

    private void ChangeState(GameState newState)
    {
        _gameState = newState;

        switch (_gameState)
        {
            case GameState.GenerateLevel:
                GenerateGrid();
                break;
            case GameState.SpawnBlocks:
                SpawnBlocks(_round++ == 0 ? 2 : 1);
                break;
            case GameState.Move:
                break;
            case GameState.Lose:
                _looseScreen.SetActive(true);
                break;
            case GameState.Win:
                _winScreen.SetActive(true);
                break;
            case GameState.WaitInput:
                break;
        }
    }

    private void GenerateGrid()
    {
        for (int x = 0; x < _size; x++)
            for (int y = 0; y < _size; y++)
            {
                var node = Instantiate(_nodePrefab, new Vector2(x, y), Quaternion.identity);
                _nodes.Add(node);
            }
        var center = new Vector2((float)_size / 2 - 0.5f, (float)_size / 2 - 0.5f);

        var board = Instantiate(_boardPrefab, center, Quaternion.identity);
        board.size = new Vector2(_size, _size);

        Camera.main.transform.position = new Vector3(center.x, center.y, -10);

        ChangeState(GameState.SpawnBlocks);
    }

    private void SpawnBlocks(int amount)
    {
        var freeNodes = _nodes.Where(n => n.OcupiedBlock == null).OrderBy(b => Random.value);

        foreach (var node in freeNodes.Take(amount))
            SpawnBlock(node, Random.value > 0.9f ? 4 : 2);

        if (freeNodes.Count() == 1)
        {
            ChangeState(GameState.Lose);
            return;
        }

        ChangeState(_blocks.Any(b => b.Value == _winCondition) ? GameState.Win : GameState.WaitInput);
    }

    private void SpawnBlock(Node node, int value)
    {
        var block = Instantiate(_blockPrefab, node.Pos, Quaternion.identity);
        block.Init(GetBlockTypeByValue(value));
        block.SetBlock(node);
        _blocks.Add(block);
    }

    private void Update()
    {
        if (_gameState != GameState.WaitInput)
            return;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            Shift(Vector2.left);
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            Shift(Vector2.right);
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            Shift(Vector2.down);
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            Shift(Vector2.up);
    }

    private void Shift(Vector2 direction)
    {
        ChangeState(GameState.Move);
        var orderedBlocks = _blocks.OrderBy(b => b.Pos.x).ThenBy(b => b.Pos.y).ToList();

        if (direction == Vector2.right ||
            direction == Vector2.up)
            orderedBlocks.Reverse();

        foreach (var block in orderedBlocks)
        {
            var next = block.Node;

            do
            {
                block.SetBlock(next);

                var possibleNode = GetNodeAtPos(next.Pos + direction);
                if (possibleNode != null)
                {
                    if (possibleNode.OcupiedBlock != null && possibleNode.OcupiedBlock.CanMerge(block.Value))
                        block.MergeBlock(possibleNode.OcupiedBlock);
                    else if (possibleNode.OcupiedBlock == null)
                        next = possibleNode;
                }
            } while (next != block.Node);
        }

        var sequence = DOTween.Sequence();

        foreach (var block in orderedBlocks)
        {
            var movePoint = block.MergingBlock != null ? block.MergingBlock.Node.Pos : block.Node.Pos;

            sequence.Insert(0, block.transform.DOMove(movePoint, _travelTime));
        }

        sequence.OnComplete(() =>
        {
            foreach (var block in orderedBlocks.Where(b => b.MergingBlock != null))
                MergeBlocks(block.MergingBlock, block);

            ChangeState(GameState.SpawnBlocks);
        });
    }

    private void MergeBlocks(Block baseBlock, Block mergingBlock)
    {
        SpawnBlock(baseBlock.Node, baseBlock.Value * 2);

        RemoveBlock(mergingBlock);
        RemoveBlock(baseBlock);
    }

    private void RemoveBlock(Block block)
    {
        _blocks.Remove(block);
        Destroy(block.gameObject);
    }

    private Node GetNodeAtPos(Vector2 pos)
    {
        return _nodes.FirstOrDefault(n => n.Pos == pos);
    }
}

public enum GameState
{
    GenerateLevel,
    SpawnBlocks,
    WaitInput,
    Move,
    Win,
    Lose
}