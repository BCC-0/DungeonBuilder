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
    /// <summary>
    /// The width used while measuring text, large enough that it never wraps.
    /// </summary>
    private const float MeasureWidthLimit = 10000f;

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
    private Color caretColor = Color.white;

    private TMP_Text target;
    private float blinkTimer;
    private bool caretVisible = true;
    private int lastCaretPosition = -1;
    private bool lastFocused;
    private bool keepWordsWhole;

    /// <summary>
    /// Gets or sets a value indicating whether words are kept whole when the
    /// text is laid out. A word that does not fit moves to the next line, and
    /// a word that is wider than a line is broken with hyphens.
    /// </summary>
    /// <value>True to wrap without splitting words.</value>
    public bool KeepWordsWhole
    {
        get
        {
            return this.keepWordsWhole;
        }

        set
        {
            if (this.keepWordsWhole == value)
            {
                return;
            }

            this.keepWordsWhole = value;

            if (this.spriteAsset != null)
            {
                this.Refresh();
            }
        }
    }

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
        string text = this.keepWordsWhole && this.inputField == null
            ? this.WrapText(this.sourceText)
            : this.sourceText;

        this.Target.spriteAsset = this.spriteAsset;
        this.Target.text = this.Convert(text, caretIndex);
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

    /// <summary>
    /// Wraps the text so words are never split. A word that does not fit on
    /// the current line moves to the next line, and a word that is wider than
    /// a whole line is broken up with hyphens.
    /// </summary>
    /// <param name="text">Plain text to wrap.</param>
    /// <returns>The text with manual line breaks.</returns>
    private string WrapText(string text)
    {
        float maxWidth = this.GetWrapWidth();

        if (string.IsNullOrEmpty(text) || maxWidth <= 0f)
        {
            return text;
        }

        string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        string[] paragraphs = normalized.Split('\n');
        StringBuilder builder = new StringBuilder(normalized.Length + 16);

        for (int i = 0; i < paragraphs.Length; i++)
        {
            if (i > 0)
            {
                builder.Append('\n');
            }

            this.AppendWrappedParagraph(builder, paragraphs[i], maxWidth);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Appends one line of already broken text. Words are moved to the next
    /// line when they do not fit, and hyphenated when they are wider than a
    /// line on their own.
    /// </summary>
    /// <param name="builder">Builder receiving the wrapped paragraph.</param>
    /// <param name="paragraph">The paragraph without line breaks.</param>
    /// <param name="maxWidth">The width a line may use.</param>
    private void AppendWrappedParagraph(
        StringBuilder builder,
        string paragraph,
        float maxWidth)
    {
        string[] words = paragraph.Split(' ');
        string line = string.Empty;

        foreach (string word in words)
        {
            if (string.IsNullOrEmpty(word))
            {
                continue;
            }

            string candidate = line.Length == 0
                ? word
                : line + " " + word;

            if (this.MeasureWidth(candidate) <= maxWidth)
            {
                line = candidate;
                continue;
            }

            if (line.Length > 0)
            {
                builder.Append(line);
                builder.Append('\n');
                line = string.Empty;
            }

            line = this.Hyphenate(builder, word, maxWidth);
        }

        builder.Append(line);
    }

    /// <summary>
    /// Breaks a word that is wider than a line into hyphenated parts, so the
    /// word is only split at a visible hyphen.
    /// </summary>
    /// <param name="builder">Builder receiving the finished parts.</param>
    /// <param name="word">The word to break up.</param>
    /// <param name="maxWidth">The width a line may use.</param>
    /// <returns>The last part of the word, which still fits on a line.</returns>
    private string Hyphenate(
        StringBuilder builder,
        string word,
        float maxWidth)
    {
        string remaining = word;

        while (remaining.Length > 0 &&
               this.MeasureWidth(remaining) > maxWidth)
        {
            int length = remaining.Length - 1;

            while (length > 1 &&
                   this.MeasureWidth(
                       remaining.Substring(0, length) + "-") > maxWidth)
            {
                length--;
            }

            if (length <= 1)
            {
                // Not even one character fits next to a hyphen, so let the
                // text overflow instead of looping forever.
                return remaining;
            }

            builder.Append(remaining, 0, length);
            builder.Append('-');
            builder.Append('\n');

            remaining = remaining.Substring(length);
        }

        return remaining;
    }

    /// <summary>
    /// Measures how wide the given plain text is when it is drawn with the
    /// sprite font, without wrapping it.
    /// </summary>
    /// <param name="text">Plain text to measure.</param>
    /// <returns>The width in local units.</returns>
    private float MeasureWidth(string text)
    {
        if (string.IsNullOrEmpty(text) ||
            this.Target == null ||
            this.spriteAsset == null)
        {
            return 0f;
        }

        string converted = this.Convert(text, -1);

        return this.Target.GetPreferredValues(
            converted,
            MeasureWidthLimit,
            MeasureWidthLimit).x;
    }

    /// <summary>
    /// Gets the horizontal space a line may use before it wraps.
    /// </summary>
    /// <returns>The available width in local units.</returns>
    private float GetWrapWidth()
    {
        if (this.Target == null)
        {
            return 0f;
        }

        Vector4 margins = this.Target.margin;

        return this.Target.rectTransform.rect.width - margins.x - margins.z;
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