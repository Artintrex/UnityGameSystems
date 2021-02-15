using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Status : MonoBehaviour
{
    [System.Flags] 
    public enum StatusMode : uint
    {
        None           = 0,
        Water          = 1 << 0,
        Bubble         = 1 << 1,
        Damaged        = 1 << 2
    }

    public uint CharacterStatus;

    public void AddStatus(StatusMode status)
    {
        CharacterStatus |= (uint)status;
    }

    public void RemoveStatus(StatusMode status)
    {
        CharacterStatus &= ~(uint)status;
    }

    public bool HasStatus(StatusMode status)
    {
        return (CharacterStatus & (uint)status) == (uint)status;
    }
}
