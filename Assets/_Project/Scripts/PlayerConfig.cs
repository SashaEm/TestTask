using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Configs/PlayerConfig")]
public class PlayerConfig : ScriptableObject
{
    [field: SerializeField] public float InitialPlayerRadius { get; private set; } = 1f;
}