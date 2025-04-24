using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewInstructionSet", menuName = "Instructions/Instruction Set")]
public class InstructionSet : ScriptableObject
{
    public List<Instruction> instructions = new List<Instruction>();
}
