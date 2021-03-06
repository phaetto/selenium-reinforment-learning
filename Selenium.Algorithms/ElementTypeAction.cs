﻿namespace Selenium.Algorithms
{
    using OpenQA.Selenium;
    using Selenium.Algorithms.ReinforcementLearning;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public sealed class ElementTypeAction : IAgentAction<IReadOnlyCollection<ElementData>>
    {
        public static readonly ElementTypeAction NoTypeAction = new ElementTypeAction();

        private readonly ElementData webElement;
        private readonly string text;
        public readonly string CachedName;
        public readonly int CachedHash;

        public ElementTypeAction(in ElementData webElement, in string text)
        {
            this.webElement = webElement;
            this.text = text;

            // We have to cache those values because the element will get out of the DOM eventually
            CachedHash = webElement.ExtendedGetHashCode();
            CachedHash = (CachedHash * 7) + text.GetHashCode();
            CachedName = $"Type '{text}' on {webElement.GetQuery().Query}";
        }

        private ElementTypeAction()
        {
            text = "No type action";
            CachedName = string.Empty;
            CachedHash = 0;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<IState<IReadOnlyCollection<ElementData>>> ExecuteAction(IEnvironment<IReadOnlyCollection<ElementData>> environment, IState<IReadOnlyCollection<ElementData>> state)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var seleniumEnvironment = (SeleniumEnvironment)environment;
            try
            {
                seleniumEnvironment.Options.WriteLine($"\t{CachedName}");
                webElement.WebElementReference.Clear();
                webElement.WebElementReference.SendKeys(text);
                seleniumEnvironment.Options.WriteLine($"\t\t... done!");
            }
            catch (ElementNotInteractableException)
            {
                seleniumEnvironment.Options.WriteLine($"\t\t... failed: non-interactable");
                // Do not move at all if we cannot click, it should penalize it
            }
            catch (StaleElementReferenceException)
            {
                seleniumEnvironment.Options.WriteLine($"\t\t... failed: stale");
                // Do not move at all if we cannot click, it should penalize it
            }

            return await environment.GetCurrentState();
        }

        public override bool Equals(object obj)
        {
            return obj is ElementTypeAction otherAction
                && CachedHash == otherAction.CachedHash;
        }

        public override int GetHashCode()
        {
            return CachedHash;
        }

        public override string ToString()
        {
            return CachedName;
        }
    }
}
