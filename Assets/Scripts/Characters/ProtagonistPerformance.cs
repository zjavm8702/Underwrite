using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Underwrite.Characters
{
    public enum ProtagonistEmotion { Neutral, Blink, Happy, Anxious, Angry, Shocked, Sad, Relieved }
    public enum ProtagonistMotion { Idle, Walking, Celebrate }

    public sealed class ProtagonistPerformance : MonoBehaviour
    {
        public Animator animator;
        public SpriteResolver eyes, mouth, rightHand, leftHand;
        public ProtagonistEmotion emotion;
        public ProtagonistMotion motion;
        public bool fists;
        public bool automaticPreview = true;
        ProtagonistMotion previousMotion;
        bool started;
        float began;

        void OnEnable(){began=Time.unscaledTime;started=false;}
        void Update()
        {
            if(automaticPreview)
            {
                int stage=Mathf.FloorToInt((Time.unscaledTime-began)/3.2f)%6;
                motion=stage==1?ProtagonistMotion.Walking:stage==2||stage==4?ProtagonistMotion.Celebrate:ProtagonistMotion.Idle;
                emotion=stage==2?ProtagonistEmotion.Happy:stage==3?ProtagonistEmotion.Anxious:stage==4?ProtagonistEmotion.Angry:stage==5?ProtagonistEmotion.Relieved:ProtagonistEmotion.Neutral;
                fists=stage==2||stage==4;
            }
            ApplyAppearance(emotion,fists);
            if(animator&&(!started||previousMotion!=motion))
            {
                animator.Play(motion.ToString(),0,0);previousMotion=motion;started=true;
            }
        }
        public void ApplyAppearance(ProtagonistEmotion value,bool closedHands)
        {
            eyes.SetCategoryAndLabel("Eyes",value.ToString());mouth.SetCategoryAndLabel("Mouth",value.ToString());
            rightHand.SetCategoryAndLabel("Hand_R",closedHands?"Fist":"Open");leftHand.SetCategoryAndLabel("Hand_L",closedHands?"Fist":"Open");
            eyes.ResolveSpriteToSpriteRenderer();mouth.ResolveSpriteToSpriteRenderer();rightHand.ResolveSpriteToSpriteRenderer();leftHand.ResolveSpriteToSpriteRenderer();
        }
    }
}
