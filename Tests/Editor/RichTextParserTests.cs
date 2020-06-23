using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

using static Unity.InteractiveTutorials.RichTextParser;

namespace Unity.InteractiveTutorials.Tests
{
    public class RichTextParserTests 
    {

        //public TutorialWindow m_tutorialWindow;

        //string loremIpsum = "Lorem Ipsum is simply dummy text of the printing and typesetting industry. " +
        //    "Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown" +
        //    " printer took a galley of type and scrambled it to make a type specimen book. It has survived" +
        //    " not only five centuries, but also the leap into electronic typesetting, remaining essentially" +
        //    " unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem" +
        //    " Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including" +
        //    " versions of Lorem Ipsum.";

        //string Bold (string toBold)
        //{
        //    boldUsed++;
        //    return " <b>" + toBold + "</b> ";
        //}

        //string Italic(string toBold)
        //{
        //    italicUsed++;
        //    return " <i>" + toBold + "</i> ";
        //}

        //string Link(string URL, string Text)
        //{
        //    linksUsed++;
        //    return " <a href=" + '\"' + URL + '\"' + ">" + Text + "</a> ";  
        //}

        //string EmptyParagraph ()
        //{
        //    paragraphs++;
        //    return "\n\n";
        //}

        //int boldUsed = 0;
        //int italicUsed = 0;
        //int linksUsed = 0;
        //int paragraphs = 0;

        //void Reset()
        //{
        //    boldUsed = 0;
        //    italicUsed = 0;
        //    linksUsed = 0;
        //    paragraphs = 0;
        //}

        //[SetUp]
        //public void SetUp ()
        //{
        //    Reset();
        //    m_tutorialWindow = TutorialWindow.CreateWindow<TutorialWindow>();
        //    m_tutorialWindow.rootVisualElement.style.flexDirection = FlexDirection.Row;
        //    m_tutorialWindow.rootVisualElement.style.flexWrap = Wrap.Wrap;
        //}

        //[TearDown]
        //public void TearDown()
        //{
        //    m_tutorialWindow.Close();
        //}

        //[Test]
        //public void CanCreateWrappingTextLabels()
        //{
        //    Reset();
        //    RichTextToVisualElements(CreateRichText(50,0,0,0), m_tutorialWindow.rootVisualElement);
            
        //    Assert.IsTrue(DoStylesMatch());
        //}

        //[Test]
        //public void CanCreateRichText()
        //{
        //    Reset();
        //    RichTextToVisualElements(CreateRichText(50, 10, 10, 5), m_tutorialWindow.rootVisualElement);
        //    Assert.IsTrue(DoStylesMatch());
        //}

        //[Test]
        //public void CanCreateParagraphsOfRichText()
        //{
        //    Reset();
        //    string richText = "";
        //    for (int i = 0; i < 10;i++)
        //    {
        //        richText += CreateRichText(i*10, i*2, i, i) + EmptyParagraph();
        //    }
        //    RichTextToVisualElements(richText, m_tutorialWindow.rootVisualElement);
        //    Assert.IsTrue(DoStylesMatch());
        //}

        //bool DoStylesMatch()
        //{
        //    int boldFound = countStyles(FontStyle.Bold);
        //    int italicFound = countStyles(FontStyle.Italic);
            
        //    if (boldFound != boldUsed)
        //    {
        //        Debug.LogError("Invalid amount of bold words. Entered: " + boldUsed + " - Found: " + boldFound);
        //        return false;
        //    }

        //    if (italicFound != italicUsed)
        //    {
        //        Debug.LogError("Invalid amount of italic words. Entered: " + italicUsed + " - Found: " + italicFound);
        //        return false;
        //    }
        //    return true;
        //}


        //int countWordLabels()
        //{
        //    var root = m_tutorialWindow.rootVisualElement;
        //    return root.childCount;
        //}

        //int countStyles(StyleEnum<FontStyle> style)
        //{
        //    var root = m_tutorialWindow.rootVisualElement;
        //    int styledWords = 0;
        //    for (int i = 0; i < root.childCount; i++)
        //    {
        //        if(root.ElementAt(i).style.unityFontStyleAndWeight == style)
        //        {
        //            styledWords++;
        //        }
        //    }
        //    return styledWords;
        //}

        //string CreateRichText(int normalWords, int boldedWords, int italicWords, int links)
        //{
        //    string richText = "";
        //    int wordNumber = 0;
        //    string[] loremIpsums = loremIpsum.Split(' ');
        //    while (normalWords > 0 || boldedWords > 0 || italicWords > 0 || links > 0)
        //    {
        //        if (normalWords >0)
        //        {
        //            richText += loremIpsums[wordNumber] + " ";
        //            normalWords--;
        //            wordNumber = (wordNumber + 1) % loremIpsums.Length;
        //        }
        //        if (boldedWords >0)
        //        {
        //            richText += Bold(loremIpsums[wordNumber]);
        //            boldedWords--;
        //            wordNumber = (wordNumber + 1) % loremIpsums.Length;
        //        }
        //        if (italicWords > 0)
        //        {
        //            richText += Italic(loremIpsums[wordNumber]);
        //            italicWords--;
        //            wordNumber = (wordNumber + 1) % loremIpsums.Length;
        //        }
        //        if (links > 0)
        //        {
        //            richText += Link("http://www.google.fi", loremIpsums[wordNumber]);
        //            links--;
        //            wordNumber = (wordNumber + 1) % loremIpsums.Length;
        //        }
        //    }
        //    return richText;
        //}

    }
}
