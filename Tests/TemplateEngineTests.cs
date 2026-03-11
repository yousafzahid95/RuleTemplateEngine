using NUnit.Framework;
using RuleTemplateEngine.Helpers;
using RuleTemplateEngine.Events;
using RuleTemplateEngine.Models;
using RuleTemplateEngine.TemplateEngine;
using System.Linq;

namespace RuleTemplateEngine.Tests
{
    [TestFixture]
    public class TemplateEngineTests
    {
        [Test]
        public void Resolve_ShouldReturnEnumName_WhenEnumPresent()
        {
            var externalMsg = new ExternalGenericEntityEventMessage
            {
                MessageId = Guid.NewGuid(),
                EventType = "Test1",
                Label = "Label1",
                Timestamp = DateTime.UtcNow,
                Body = new ActionItemClosedData() { WorkAreaId = Guid.NewGuid(), Reason = CloseReason.Cancelled },
                MessageHeaders = new MessageHeadersFields() { ReplyTo = "test" }
            };

            var dataset = TransformToIDataRecord<ExternalGenericEntityEventMessage>.TransformFromObject(externalMsg, "Key").ToList();
            var tpl = new TemplateParam { Template = "{0}", Params = new System.Collections.Generic.List<string> { "[Key.Body.Reason]" } };

            var resolved = RuleTemplateEngine.Resolve(tpl, dataset);

            Assert.That(resolved, Is.EqualTo("Cancelled"));
        }

        [Test]
        public void Resolve_ShouldReturnString_WhenStringValue()
        {
            var msg = new ExternalGenericEntityEventMessage
            {
                MessageId = Guid.NewGuid(),
                EventType = "Test2",
                Label = "L2",
                Timestamp = DateTime.UtcNow,
                Body = new { Text = "Hello" }
            };

            var dataset = TransformToIDataRecord.TransformFromObject(msg, "Key").ToList();
            var tpl = new TemplateParam { Template = "{0}", Params = new System.Collections.Generic.List<string> { "[Key.Body.Text]" } };

            var resolved = RuleTemplateEngine.Resolve(tpl, dataset);

            Assert.That(resolved, Is.EqualTo("Hello"));
        }

        [Test]
        public void ExpressionResolver_ShouldReturnEnumObject_WhenEnumPresent()
        {
            var externalMsg = new ExternalGenericEntityEventMessage
            {
                MessageId = Guid.NewGuid(),
                EventType = "Test3",
                Label = "Label3",
                Timestamp = DateTime.UtcNow,
                Body = new ActionItemClosedData() { WorkAreaId = Guid.NewGuid(), Reason = CloseReason.Completed },
                MessageHeaders = new MessageHeadersFields() { ReplyTo = "test" }
            };

            var dataset = TransformToIDataRecord<ExternalGenericEntityEventMessage>.TransformFromObject(externalMsg, "Key").ToList();
            var keyed = RuleTemplateEngine.BuildKeyedDataset(dataset);

            var value = ExpressionResolver.Resolve("[Key.Body.Reason]", keyed);

            Assert.That(value, Is.Not.Null);
            Assert.That(value.GetType().Name, Is.EqualTo(typeof(CloseReason).Name));
            Assert.That(value.ToString(), Is.EqualTo(CloseReason.Completed.ToString()));
        }
    }
}
