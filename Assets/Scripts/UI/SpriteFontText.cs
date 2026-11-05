using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Allows typing text to be turned into a custom sprite asset font.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class SpriteFontText : MonoBehaviour
{
    [SerializeField]
    private TMP_SpriteAsset spriteAsset;
    [SerializeField]
    private TMP_InputField inputField;
    [SerializeField]
    [TextArea(3, 10)]
    private string sourceText;
    [SerializeField]
    private string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.,!?'\":;-+_=()[]/\\#|";
    [SerializeField]
    private float spaceWidthEm = 0.5f;
    [SerializeField]
    private float letterSpacingEm = 0.1f;
    [SerializeField]
    private bool ignoreCase = true;
    [SerializeField]
    private char caretCharacter = '|';
    [SerializeField]
    private float caretBlinkSeconds = 0.5f;
    [SerializeField]
    private Color caretColor;

    private TMP_Text target;
    private float blinkTimer;
    private bool caretVisible = true;
    private int lastCaretPosition = -1;
    private bool lastFocused;

    /// <returns>The text component this script writes to.</returns>
    private TMP_Text Target
    {
        get
        {
            if (this.target == null)
            {
                this.target = this.GetComponent<TMP_Text>();
            }

            return this.target;
        }
    }

    /// <returns>True if a caret should currently be part of the rendered text.</returns>
    private bool ShowCaret
    {
        get
        {
            return Application.isPlaying && this.inputField != null && this.inputField.isFocused;
        }
    }

    /// <summary>
    /// Sets the text.
    /// </summary>
    /// <param name="text">Plain text to display using the sprite font.</param>
    public void SetText(string text)
    {
        this.sourceText = text;
        this.Refresh();
    }

    /// <summary>
    /// Rebuilds the target text from the current source text.
    /// </summary>
    public void Refresh()
    {
        if (this.spriteAsset == null || this.Target == null)
        {
            Debug.LogWarning($"[SpriteFontText] Refresh aborted: spriteAsset null={this.spriteAsset == null}, target null={this.Target == null}", this);
            return;
        }

        int caretIndex = this.ShowCaret ? Mathf.Clamp(this.inputField.stringPosition, 0, this.sourceText.Length) : -1;
        this.Target.spriteAsset = this.spriteAsset;
        this.Target.text = this.Convert(this.sourceText, caretIndex);
    }

    /// <param name="builder">Builder receiving the output.</param>
    /// <param name="token">The tag to append.</param>
    /// <param name="gap">The letter spacing tag placed between tokens.</param>
    /// <param name="first">True if nothing has been appended on the current line yet; set to false afterwards.</param>
    private static void AppendToken(StringBuilder builder, string token, string gap, ref bool first)
    {
        if (!first)
        {
            builder.Append(gap);
        }

        builder.Append(token);
        first = false;
    }

    /// <summary>
    /// Converts a given text string into the sprite font asset.
    /// </summary>
    /// <param name="text">Plain text to convert.</param>
    /// <param name="caretIndex">Index in the plain text before which the caret is inserted, or -1 for no caret.</param>
    /// <returns>The text with sprite tags, with letter spacing applied between characters but not after the last character of a line. Characters without a sprite are removed.</returns>
    private string Convert(string text, int caretIndex)
    {
        if (string.IsNullOrEmpty(text) && caretIndex < 0)
        {
            return string.Empty;
        }

        text = text ?? string.Empty;
        var lookup = this.BuildLookup();
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        string gap = "<space=" + this.letterSpacingEm.ToString(CultureInfo.InvariantCulture) + "em>";
        string space = "<space=" + this.spaceWidthEm.ToString(CultureInfo.InvariantCulture) + "em>";
        string caret = this.BuildCaretToken(lookup);
        var builder = new StringBuilder(text.Length * 20);
        int globalIndex = 0;

        for (int l = 0; l < lines.Length; l++)
        {
            bool first = true;
            string line = lines[l];

            for (int i = 0; i < line.Length; i++)
            {
                if (globalIndex == caretIndex && caret != null)
                {
                    AppendToken(builder, caret, gap, ref first);
                }

                char c = line[i];
                globalIndex++;

                if (c == ' ')
                {
                    AppendToken(builder, space, gap, ref first);
                }
                else if (this.TryGetIndex(lookup, c, out int index))
                {
                    AppendToken(builder, "<sprite=" + index + " tint=1>", gap, ref first);
                }
            }

            if (globalIndex == caretIndex && caret != null)
            {
                AppendToken(builder, caret, gap, ref first);
            }

            if (l < lines.Length - 1)
            {
                builder.Append('\n');
                globalIndex++;
            }
        }

        return builder.ToString();
    }

    /// <param name="lookup">Character to sprite index map.</param>
    /// <returns>The caret tag, invisible but space-preserving while blinking off, or null if the caret character has no sprite.</returns>
    private string BuildCaretToken(Dictionary<char, int> lookup)
    {
        if (!lookup.TryGetValue(this.caretCharacter, out int index))
        {
            return null;
        }

        Color color = this.caretColor;
        if (!this.caretVisible)
        {
            color.a = 0f;
        }

        string hexColor = "#" + ColorUtility.ToHtmlStringRGBA(color);

        return "<color=" + hexColor + "><sprite=" + index + " tint=1></color>";
    }

    /// <returns>A dictionary from character to sprite index, based on position in the characters string.</returns>
    private Dictionary<char, int> BuildLookup()
    {
        var lookup = new Dictionary<char, int>();
        for (int i = 0; i < this.characters.Length; i++)
        {
            lookup.TryAdd(this.characters[i], i);
        }

        return lookup;
    }

    /// <param name="lookup">Character to look up.</param>
    /// <param name="c">Character to look up the index.</param>
    /// <param name="index">The sprite index when found.</param>
    /// <returns>True if the character has a sprite.</returns>
    private bool TryGetIndex(Dictionary<char, int> lookup, char c, out int index)
    {
        if (c == this.caretCharacter && !lookup.ContainsKey(c))
        {
            index = -1;
            return false;
        }

        if (lookup.TryGetValue(c, out index))
        {
            return true;
        }

        if (!this.ignoreCase)
        {
            return false;
        }

        return lookup.TryGetValue(char.ToUpperInvariant(c), out index)
            || lookup.TryGetValue(char.ToLowerInvariant(c), out index);
    }

    /// <param name="value">The current plain text of the input field.</param>
    private void OnInputChanged(string value)
    {
        this.blinkTimer = 0f;
        this.caretVisible = true;
        this.SetText(value);
    }

    private void HideInputVisuals()
    {
        if (this.inputField == null)
        {
            return;
        }

        this.inputField.customCaretColor = true;
        Color caretColor = this.inputField.caretColor;
        caretColor.a = 0f;
        this.inputField.caretColor = caretColor;

        if (this.inputField.textComponent == null || this.inputField.textComponent == this.Target)
        {
            return;
        }

        Color color = this.inputField.textComponent.color;
        color.a = 0f;
        this.inputField.textComponent.color = color;
    }

    private void Update()
    {
        if (!Application.isPlaying || this.inputField == null)
        {
            return;
        }

        bool focused = this.inputField.isFocused;
        int caretPosition = this.inputField.stringPosition;
        bool changed = focused != this.lastFocused || (focused && caretPosition != this.lastCaretPosition);

        if (focused)
        {
            this.blinkTimer += Time.unscaledDeltaTime;
            if (changed)
            {
                this.blinkTimer = 0f;
                this.caretVisible = true;
            }
            else if (this.blinkTimer >= this.caretBlinkSeconds)
            {
                this.blinkTimer = 0f;
                this.caretVisible = !this.caretVisible;
                changed = true;
            }
        }

        this.lastFocused = focused;
        this.lastCaretPosition = caretPosition;

        if (changed)
        {
            this.Refresh();
        }
    }

    private void OnEnable()
    {
        if (this.inputField != null && Application.isPlaying)
        {
            this.inputField.onValueChanged.AddListener(this.OnInputChanged);
            this.sourceText = this.inputField.text;
            this.HideInputVisuals();
        }

        this.Refresh();
    }

    private void OnDisable()
    {
        if (this.inputField != null)
        {
            this.inputField.onValueChanged.RemoveListener(this.OnInputChanged);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null)
            {
                this.Refresh();
            }
        };
    }
#endif
}