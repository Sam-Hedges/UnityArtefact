using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private TextMeshPro homeText, awayText;
    private int homeScore, awayScore;

    public void AddScore(string tag, Vector3 velocity) {
        if (velocity.y < 0) {
            switch (tag) {
                case "Home":
                    homeScore += 2;
                    break;
                case "Away":
                    awayScore += 2;
                    break;
            }
            UpdateText();
        }
    }

    private void UpdateText() {
        string ht = homeScore.ToString();
        switch (ht.Length) {
            case 1:
                ht = "00" + ht;
                break;
            case 2:
                ht = "0" + ht;
                break;
        }
        homeText.text = ht;
        string at = awayScore.ToString();
        switch (at.Length) {
            case 1:
                at = "00" + at;
                break;
            case 2:
                at = "0" + at;
                break;
        }
        awayText.text = at;
    }
}
