using UnityEngine;
using AbeAttributes;

public class AttributeTest : MonoBehaviour
{
    private float _hp = 10;

    [PopToConsole(
    PopToConsoleMode.Set,
    nameof(BreakIfLessThanZero))]
    public float HP
    {
        get => _hp;
        set => _hp = value;
    }

    private bool BreakIfLessThanZero(float value)
    {
        return value <= 0;
    }

    private void Start()
    {
        HP = 10;
    }

    void Update()
    {
        HP -= Time.deltaTime;
    }
}