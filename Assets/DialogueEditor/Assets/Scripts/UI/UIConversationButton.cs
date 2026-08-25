using UnityEngine;
using UnityEngine.UI;

namespace DialogueEditor
{
    public class UIConversationButton : MonoBehaviour
    {
        public enum eHoverState
        {
            idleOff,
            animatingOn,
            idleOn,
            animatingOff,
        }

        public enum eButtonType
        {
            Option,
            Speech,
            End
        }

        public eButtonType ButtonType { get { return m_buttonType; } }

        [SerializeField] private TMPro.TextMeshProUGUI TextMesh = null;
        [SerializeField] private Image OptionBackgroundImage = null;

        private eButtonType m_buttonType;
        private ConversationNode m_node;
        private eHoverState m_hoverState = eHoverState.idleOff;

        private void Awake()
        {
            // Visual hover/press animation is intentionally handled by DialogueOptionButtonAnimator.
            // Keep the pack button at neutral scale from its first frame so SetSelectedOption(0)
            // cannot visually auto-punch the first option.
            transform.localScale = Vector3.one;
        }

        private void OnEnable()
        {
            transform.localScale = Vector3.one;
        }

        public void OnHover(bool hovering)
        {
            if (!ConversationManager.Instance.AllowMouseInteraction) { return; }

            if (hovering)
                ConversationManager.Instance.AlertHover(this);
            else
                ConversationManager.Instance.AlertHover(null);
        }

        public void OnClick()
        {
            if (!ConversationManager.Instance.AllowMouseInteraction) { return; }
            DoClickBehaviour();
        }

        public void OnButtonPressed()
        {
            DoClickBehaviour();
        }

        public void SetHovering(bool selected)
        {
            // Keep DialogueEditor's logical selection state, but never touch scale/color here.
            // This prevents its built-in 1.2x hover animation from fighting the custom game UI animator.
            m_hoverState = selected ? eHoverState.idleOn : eHoverState.idleOff;
        }

        public void SetImage(Sprite sprite, bool sliced)
        {
            if (sprite == null) return;
            OptionBackgroundImage.sprite = sprite;
            OptionBackgroundImage.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        }

        public void InitButton(OptionNode option)
        {
            TextMesh.font = option.TMPFont != null ? option.TMPFont : null;
        }

        public void SetAlpha(float a)
        {
            Color imageColor = OptionBackgroundImage.color;
            Color textColor = TextMesh.color;
            imageColor.a = a;
            textColor.a = a;
            OptionBackgroundImage.color = imageColor;
            TextMesh.color = textColor;
        }

        public void SetupButton(eButtonType buttonType, ConversationNode node, TMPro.TMP_FontAsset continueFont = null, TMPro.TMP_FontAsset endFont = null)
        {
            m_buttonType = buttonType;
            m_node = node;

            switch (m_buttonType)
            {
                case eButtonType.Option:
                    TextMesh.text = node.Text;
                    TextMesh.font = node.TMPFont;
                    break;
                case eButtonType.Speech:
                    TextMesh.text = "Continue.";
                    TextMesh.font = continueFont;
                    break;
                case eButtonType.End:
                    TextMesh.text = "End.";
                    TextMesh.font = endFont;
                    break;
            }
        }

        private void DoClickBehaviour()
        {
            switch (m_buttonType)
            {
                case eButtonType.Speech:
                    ConversationManager.Instance.SpeechSelected(m_node as SpeechNode);
                    break;
                case eButtonType.Option:
                    ConversationManager.Instance.OptionSelected(m_node as OptionNode);
                    break;
                case eButtonType.End:
                    ConversationManager.Instance.EndButtonSelected();
                    break;
            }
        }
    }
}
