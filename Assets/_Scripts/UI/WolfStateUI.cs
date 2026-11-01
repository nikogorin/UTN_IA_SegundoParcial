using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WolfStateUI : StateUI
{
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private Image channelingSlideImage;

    public override void SetState(Enum state)
    {
        if (state is WolfState preyState)
        {
            stateText.text = preyState.ToString();

            stateText.color = preyState switch
            {
                WolfState.Patrol => Color.orange,
                WolfState.Investigate => Color.yellow,
                WolfState.Chase => Color.red,
                _ => Color.white
            };
        }
    }

    public void SetChannelingSlide(float amount)
    {
        channelingSlideImage.fillAmount = amount;
    }

}
