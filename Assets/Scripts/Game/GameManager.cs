using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static bool Ignore { get; set; }
    public static int Size { get; set; }
    public static GameState Gamestate { get; set; }

    [SerializeField] private readonly float _travelTime = 0.2f;
    [SerializeField] private int _winCondition;
    [SerializeField] private Node _nodePrefab;
    [SerializeField] private SpriteRenderer _boardPrefab;
    [SerializeField] private Block _blockPrefab;
    [SerializeField] private List<BlockType> _blockTypes;
    [SerializeField] private GameObject _winScreen, _looseScreen, _background, _quit, _continue;
    [SerializeField] private TextMeshProUGUI _counter, _goal, _result;
    private AudioManager audioManager;

    private int _round = 0;
    private long _score = 0;
    private List<Node> _nodes = new();
    private List<Block> _blocks = new();

    private BlockType GetBlockTypeByValue(int value) => _blockTypes.First(x => x.Value == value);

    private List<Node> GetFreeNodes() => _nodes.Where(n => n.OccupiedBlock == null)
        .OrderBy(b => Random.value).ToList();

    public void Awake()
    {
        audioManager = GameObject.FindGameObjectWithTag("Sound").GetComponent<AudioManager>();
    }

    public void Start()
    {
        Ignore = false;
        switch (Size)
        {
            case 3:
                _winCondition = 128;
                break;
            case 4:
                _winCondition = 2048;
                break;
            case 5:
                _winCondition = 262144;
                break;
        }

        _goal.SetText("Goal: " + _winCondition);
        GenerateGrid();
    }

    private void ChangeState(GameState newState)
    {
        Gamestate = newState;

        switch (Gamestate)
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
                _background.SetActive(true);
                _looseScreen.SetActive(true);
                _quit.SetActive(true);
                _result.SetText("You scored " + _score.ToString());
                _result.gameObject.SetActive(true);
                break;
            case GameState.Win:
                _background.SetActive(true);
                _winScreen.SetActive(true);
                _quit.SetActive(true);
                _continue.SetActive(true);
                break;
            case GameState.WaitInput:
                break;
        }
    }

    private void GenerateGrid()
    {
        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
            {
                var node = Instantiate(_nodePrefab, new Vector2(x, y), Quaternion.identity);
                _nodes.Add(node);
            }
        var center = new Vector2((float)Size / 2 - 0.5f, (float)Size / 2 - 0.5f);

        var board = Instantiate(_boardPrefab, center, Quaternion.identity);
        board.size = new Vector2(Size, Size);

        Camera.main.transform.position = new Vector3(center.x, center.y, -10);

        ChangeState(GameState.SpawnBlocks);
    }

    private void SpawnBlocks(int amount)
    {
        if (!Ignore)
            ChangeState(_blocks.Any(b => b.Value == _winCondition)
                ? GameState.Win
                : GameState.WaitInput);
        else
            ChangeState(GameState.WaitInput);

        var freeNodes = GetFreeNodes();

        if (CheckLose(freeNodes))
        {
            ChangeState(GameState.Lose);
            return;
        }

        foreach (var node in freeNodes.Take(amount))
            SpawnBlock(node, Random.value > 0.9f ? 4 : 2);
    }

    private bool CheckLose(List<Node> freeNodes)
    {
        if (freeNodes.Count() > 0)
            return false;

        for (int index = 0; index < _nodes.Count; index += 2)
        {
            if ((index - 1) > 0 && index % Size != 0 && _nodes[index].OccupiedBlock.CanMerge(_nodes[index - 1].OccupiedBlock.Value))
                return false;
            if ((index + 1) < _nodes.Count && (index + 1) % Size != 0 && _nodes[index].OccupiedBlock.CanMerge(_nodes[index + 1].OccupiedBlock.Value))
                return false;
            if ((index + Size) < _nodes.Count && _nodes[index].OccupiedBlock.CanMerge(_nodes[index + Size].OccupiedBlock.Value))
                return false;
            if ((index - Size) > 0 && _nodes[index].OccupiedBlock.CanMerge(_nodes[index - Size].OccupiedBlock.Value))
                return false;
        }

        return true;
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
        if (Gamestate != GameState.WaitInput)
            return;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            Shift(Vector2.left);
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            Shift(Vector2.right);
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            Shift(Vector2.down);
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            Shift(Vector2.up);
        else if (Input.GetKeyDown(KeyCode.Escape))
            SceneManager.LoadSceneAsync(0);
    }

    private void Shift(Vector2 direction)
    {
        ChangeState(GameState.Move);
        var orderedBlocks = _blocks.OrderBy(b => b.Pos.x)
            .ThenBy(b => b.Pos.y).ToList();
        var moved = false;
        var merged = false;

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
                    var value = block.Value;
                    if (possibleNode.OccupiedBlock != null &&
                        possibleNode.OccupiedBlock.CanMerge(value))
                    {
                        block.MergeBlock(possibleNode.OccupiedBlock);
                        _score += value * 2;
                        _counter.SetText(_score.ToString());
                        moved = true;
                        merged = true;
                    }
                    else if (possibleNode.OccupiedBlock == null)
                    {
                        next = possibleNode;
                        moved = true;
                    }
                }
            } while (next != block.Node);
        }

        if (!moved && GetFreeNodes().Count != 0)
        {
            ChangeState(GameState.WaitInput);
            return;
        }

        if (merged)
            audioManager.PlayMerge();
        else 
            audioManager.PlayMove();

        var sequence = DOTween.Sequence();

        foreach (var block in orderedBlocks)
        {
            var movePoint = block.MergingBlock != null
                ? block.MergingBlock.Node.Pos
                : block.Node.Pos;

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