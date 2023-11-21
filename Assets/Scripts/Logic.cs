using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Logic : MonoBehaviour
{
    public const string ATTEMPTS_KEY = "Attempts";

    [SerializeField]
    private int _startingAttempts = 3;
    [SerializeField]
    private UIDice _dice;
    [SerializeField]
    private Button _btnRoll;
    [SerializeField]
    private TMP_Text _txtAttempts;
    [SerializeField]
    private TMP_InputField _numberInput;
    [SerializeField]
    private TMP_Text _txtResult;

    private int _currentAttempts = 0;
    private int _answer;
    

    void Awake()
    {
        if (PlayerPrefs.HasKey(ATTEMPTS_KEY))
            _currentAttempts = PlayerPrefs.GetInt(ATTEMPTS_KEY, 0);
        else
        {
            _currentAttempts = _startingAttempts;
        }
        UpdateAttempts(_currentAttempts);
    }

    public void OnRollDicePressed()
    {
        if (_currentAttempts <= 0)
        {
            Debug.LogError("Trying to roll without attempts, bailing...");
            return;
        }

        //Sanity keeping
        _txtResult.gameObject.SetActive(false);

        UpdateAttempts(_currentAttempts - 1);

        int diceValue = Random.Range(1, 7); //Max exclusive
        _dice.SetValue(diceValue);

        VerifyAnswerForDiceValue(diceValue);
    }

    private void VerifyAnswerForDiceValue(int diceValue)
    {
        StartCoroutine(DisplayResult(_answer == diceValue ? "Correct" : "Incorrect"));
    }

    IEnumerator DisplayResult(string result)
    {
        _txtResult.text = result;
        _txtResult.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        _txtResult.gameObject.SetActive(false);
    }

    public void OnTextChanged(string text)
    {
        _answer = int.Parse(_numberInput.text);

        _btnRoll.enabled = _currentAttempts > 0 && _answer > 0;
    }

    public void OnOSPBuyAttempts()
    {

    }

    public void OnSDKBuyAttempts()
    {

    }

    private void OnBuyAttemptsReturned()
    {
        UpdateAttempts(_currentAttempts + _startingAttempts);
    }

    private void UpdateAttempts(int val)
    {
        _currentAttempts = val;
        PlayerPrefs.SetInt(ATTEMPTS_KEY, _currentAttempts);
        _txtAttempts.text = _currentAttempts.ToString();
    }
}
