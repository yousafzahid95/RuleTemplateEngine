using NUnit.Framework;
using RuleTemplateEngine.Helpers;
using RuleTemplateEngine.Events;
using RuleTemplateEngine.Models;
using RuleTemplateEngine.TemplateEngine;
using System.Linq;
using Engine = RuleTemplateEngine.TemplateEngine.RuleTemplateEngine;

namespace RuleTemplateEngine.Tests
{
    [TestFixture]
    public class TemplateEngineTests
    {
        private sealed class Message<TBody>
        {
            public TBody Body { get; set; } = default!;
        }

        private sealed class ValueContainer
        {
            public BodyContainer Body { get; set; } = new();
        }

        private sealed class BodyContainer
        {
            public string[] Values { get; set; } = Array.Empty<string>();
        }

        [Test]
        public void Resolve_ShouldReturnEnumName_WhenEnumPresent()
        {
            var msg = new Message<ActionItemClosedData>
            {
                Body = new ActionItemClosedData
                {
                    WorkAreaId = Guid.NewGuid(),
                    Reason = CloseReason.Cancelled
                }
            };

            var dataset = TransformToIDataRecord<Message<ActionItemClosedData>>.TransformFromObject(msg, "Key").ToList();
            var tpl = new TemplateParam { Template = "{0}", Params = new System.Collections.Generic.List<string> { "[Key.Body.Reason]" } };

            var resolved = Engine.Resolve(tpl, dataset);

            Assert.That(resolved, Is.EqualTo("Cancelled"));
        }

        [Test]
        public void Resolve_ShouldReturnString_WhenStringValue()
        {
            var msg = new Message<BodyWithText>
            {
                Body = new BodyWithText { Text = "Hello" }
            };

            var dataset = TransformToIDataRecord<Message<BodyWithText>>.TransformFromObject(msg, "Key").ToList();
            var tpl = new TemplateParam { Template = "{0}", Params = new System.Collections.Generic.List<string> { "[Key.Body.Text]" } };

            var resolved = Engine.Resolve(tpl, dataset);

            Assert.That(resolved, Is.EqualTo("Hello"));
        }

        [Test]
        public void ExpressionResolver_ShouldReturnEnumObject_WhenEnumPresent()
        {
            var msg = new Message<ActionItemClosedData>
            {
                Body = new ActionItemClosedData
                {
                    WorkAreaId = Guid.NewGuid(),
                    Reason = CloseReason.Completed
                }
            };

            var dataset = TransformToIDataRecord<Message<ActionItemClosedData>>.TransformFromObject(msg, "Key").ToList();
            var keyed = Engine.BuildKeyedDataset(dataset);

            var value = ExpressionResolver.Resolve("[Key.Body.Reason]", keyed);

            Assert.That(value, Is.Not.Null);
            Assert.That(value.GetType().Name, Is.EqualTo(typeof(CloseReason).Name));
            Assert.That(value.ToString(), Is.EqualTo(CloseReason.Completed.ToString()));
        }

        private sealed class BodyWithText
        {
            public string Text { get; set; } = string.Empty;
        }
    }
}
