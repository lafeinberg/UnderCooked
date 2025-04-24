using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Instruction
{
    public string description;
    public InstructionType type;
    public string targetObject;
}

/* Not sure if this will be useful for object state and instruction state management */
public enum InstructionType
{
    DropItem,
    GrabItem,
    ChopItem,
    CookItem,
    AddCondiment,
    AssembleBurger,
    WayFind,
    Other
}