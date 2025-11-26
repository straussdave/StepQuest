using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InstructionsDisplay : MonoBehaviour
{
    private static readonly Dictionary<int, string> MonthMap = new()
    {
        { 8, "August" },
        { 9, "September" },
        { 10, "Oktober" },
        { 11, "November" },
        { 12, "Dezember" }
    };
    
    private const string DecimalTerm = "[[dec]]";
    private const string CodeLengthTerm = "[[len]]";

    [Header("Settings")]
    [SerializeField] private bool isMonthMode;
    
    [Header("UI Elements")] 
    [SerializeField] private Image professorPortrait;
    [SerializeField] private TMP_Text instructionText;
    
    [Header("Quests")]
    [SerializeField] private Quest introductionInstruction;
    [SerializeField] private Quest[] incorrectCodeInstruction;
    [SerializeField] private Quest[] codeTooShortInstruction;
    [SerializeField] private Quest correctCodeInstruction;

    private string _code;
    private int _codeLength;

    private int _incorrectCodeCounter;
    private int _tooShortCodeCounter;
    
    private void Awake()
    {

    }

    private void OnDestroy()
    {

    }
}