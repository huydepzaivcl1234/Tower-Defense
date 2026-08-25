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
        private RectTransform m_rect;

        private eButtonType m_buttonType;
        private ConversationNode m_node;

        private float m_hoverT = 0.0f;
        private eHoverState m_hoverState;
        private bool Hovering { get { return (m_hoverState == eHoverState.animatingOn || m_hoverState == eHoverState.animatingOff); } }
        private Vector3 BigSize { get { return Vector3.one * 1.2f; } }

        private void Awake()
        {
            m_rect = GetComponent<RectTransform>();
        }

        private void Update()
        {
            // When our custom dialogue option animator is present it owns ALL visual hover/press
            // feedback. Keep this legacy animation path disabled to avoid two systems fighting over
            // localScale. Input/selection behaviour from DialogueEditor is preserved.
            if (GetComponent<DialogueOptionButtonAnimator>() != null)
            {
                if (m_rect != null && m_rect.localScale != Vector3.one)
                    m_rect.localScale = Vector3.one;
                return;
            }

            if (Hovering)
            {
                m_hoverT += Time.deltaTime;
                float normalised = m_hoverT / 0.2f;
                bool done = false;
                if (normalised >= 1)
                {
                    normalised = 1;
                    done = true;
                }

                Vector3 size = Vector3.one;
                float ease = EaseOutQuart(normalised);

                switch (m_hoverState)
                {
                    case eHoverState.animatingOn:
                        size = Vector3.Lerp(Vector3.one, BigSize, ease);
                        break;
                    case eHoverState.animatingOff:
                        size = Vector3.Lerp(BigSize, Vector3.one, ease);
                        break;
                }

                m_rect.localScale = size;

                if (done)
                    m_hoverState = (m_hoverState == eHoverState.animatingOn) ? eHoverState.idleOn : eHoverState.idleOff;
            }
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
            // Selection remains functional for DialogueEditor's mouse/keyboard navigation, but when
            // our custom animator exists we do not run the pack's legacy 1.2x scale animation.
            if (GetComponent<DialogueOptionButtonAnimator>() != null)
            {
                m_hoverState = selected ? eHoverState.idleOn : eHoverState.idleOff;
                m_hoverT = 0f;
                return;
            }

            if (selected && (m_hoverState == eHoverState.animatingOn || m_hoverState == eHoverState.idleOn)) { return; }
            if (!selected && (m_hoverState == eHoverState.animatingOff || m_hoverState == eHoverState.idleOff)) { return; }

            m_hoverState = selected ? eHoverState.animatingOn : eHoverState.animatingOff;
            m_hoverT = 0f;
        }

        public void SetImage(Sprite sprite, bool sliced)
        {
            if (sprite != null)
            {
                OptionBackgroundImage.sprite = sprite;
                OptionBackgroundImage.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            }
        }

        public void InitButton(OptionNode option)
        {
            TextMesh.font = option.TMPFont != null ? option.TMPFont : null;
        }

        public void SetAlpha(float a)
        {
            Color c_image = OptionBackgroundImage.color;
            Color c_text = TextMesh.color;
            c_image.a = a;
            c_text.a = a;
            OptionBackgroundImage.color = c_image;
            TextMesh.color = c_text;
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

        private static float EaseOutQuart(float normalized)
        {
            return 1 - Mathf.Pow(1 - normalized, 4);
        }
    }
}
