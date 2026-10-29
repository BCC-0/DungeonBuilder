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
    [TextArea(3, 10)]
    private string sourceText;
    [SerializeField]
    private string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.,!?'\":;-+_=()[]/\\#";
    [SerializeField]
    private float spaceWidthEm = 0.5f;
    [SerializeField]
    private float letterSpacingEm = 0.1f;
    [SerializeField]
    private bool ignoreCase = true;

    private TMP_Text target;

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

    /// <summary>
    /// Sets the text 
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
            return;
        }

        this.Target.spriteAsset = this.spriteAsset;
        this.Target.text = this.Convert(this.sourceText);
    }

    /// <summary>
    /// Converts a given text string into the sprite font asset.
    /// </summary>
    /// <param name="text">Plain text to convert.</param>
    /// <returns>The text with sprite tags, with letter spacing applied between characters but not after the last character of a line. Characters without a sprite are removed.</returns>
    private string Convert(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var lookup = this.BuildLookup();
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        string gap = "<space=" + this.letterSpacingEm.ToString(CultureInfo.InvariantCulture) + "em>";
        string space = "<space=" + this.spaceWidthEm.ToString(CultureInfo.InvariantCulture) + "em>";
        var builder = new StringBuilder(text.Length * 20);

        for (int l = 0; l < lines.Length; l++)
        {
            bool first = true;

            foreach (char c in lines[l])
            {
                string token;
                if (c == ' ')
                {
                    token = space;
                }
                else if (this.TryGetIndex(lookup, c, out int index))
                {
                    token = "<sprite=" + index + ">";
                }
                else
                {
                    continue;
                }

                if (!first)
                {
                    builder.Append(gap);
                }

                builder.Append(token);
                first = false;
            }

            if (l < lines.Length - 1)
            {
                builder.Append('\n');
            }
        }

        return builder.ToString();
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

    /// <param name="lookup">Character to sprite index map.</param>
    /// <param name="c">Character to look up.</param>
    /// <param name="index">The sprite index when found.</param>
    /// <returns>True if the character has a sprite.</returns>
    private bool TryGetIndex(Dictionary<char, int> lookup, char c, out int index)
    {
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

    private void OnEnable()
    {
        this.Refresh();
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