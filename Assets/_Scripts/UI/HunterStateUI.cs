using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HunterStateUI : StateUI
{
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text gatherText;
    [SerializeField] private Image attackCooldownImage;
    [SerializeField] private Image attackCooldownImageBackground;
    [SerializeField] private Image baitCooldownImage;
    [SerializeField] private Image baitCooldownImageBackground;
    [SerializeField] private Image channelingSlideImage;

    private string internalGatherIndicator = "|";
    private bool wasCalledBefore = false;

    private void Start()
    {
        gatherText.enabled = false;
    }
    public override void SetState(Enum state)
    {
        if (state is HunterState hunterState)
        {
            stateText.text = hunterState.ToString();

            stateText.color = hunterState switch
            {
                HunterState.Idle => Color.black,
                HunterState.Starting => Color.orange,
                HunterState.Search => Color.yellow,
                HunterState.PlacingBait => Color.cyan,
                HunterState.Attacking => Color.red,
                HunterState.GoingToGather => Color.green,
                HunterState.Gathering => Color.violet,
                HunterState.Flee => Color.blue,
                HunterState.Dead => Color.violetRed,
                HunterState.Exit => Color.cadetBlue,
                _ => Color.white
            };
        }
    }

    public void IncreaseGatherText()
    {
        if(!wasCalledBefore)
        {
            gatherText.enabled = true;
            gatherText.text = $"{internalGatherIndicator}";
            wasCalledBefore = true;
            return;
        }
        gatherText.text += $" {internalGatherIndicator}";

    }

    public void SetAttackCooldown(float amount)
    {
        attackCooldownImage.fillAmount = amount;
        attackCooldownImageBackground.fillAmount = amount;
    }

    public void SetBaitCooldown(float amount)
    {
        baitCooldownImage.fillAmount = amount;
        baitCooldownImageBackground.fillAmount = amount;
    }

    public void SetChannelingSlide(float amount)
    {
        channelingSlideImage.fillAmount = amount;
    }
}
