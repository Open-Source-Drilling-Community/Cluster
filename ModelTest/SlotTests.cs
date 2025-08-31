using System;
using NUnit.Framework;
using NORCE.Drilling.Cluster.Model;

namespace NORCE.Drilling.Cluster.ModelTest
{
    [TestFixture]
    public class SlotTests
    {
        [Test]
        public void Slot_Defaults_AreExpected()
        {
            var slot = new Slot();

            Assert.That(slot.ID, Is.EqualTo(Guid.Empty));
            Assert.That(slot.Name, Is.Null);
            Assert.That(slot.Description, Is.Null);
            Assert.That(slot.CreationDate, Is.Null);
            Assert.That(slot.LastModificationDate, Is.Null);

            // Gaussian position properties default to null
            Assert.That(slot.Latitude, Is.Null);
            Assert.That(slot.Longitude, Is.Null);
        }

        [Test]
        public void Slot_SetBasicProperties_PersistsValues()
        {
            var id = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            var slot = new Slot
            {
                ID = id,
                Name = "Slot A",
                Description = "Test slot",
                CreationDate = now,
                LastModificationDate = now.AddMinutes(1)
            };

            Assert.That(slot.ID, Is.EqualTo(id));
            Assert.That(slot.Name, Is.EqualTo("Slot A"));
            Assert.That(slot.Description, Is.EqualTo("Test slot"));
            Assert.That(slot.CreationDate, Is.EqualTo(now));
            Assert.That(slot.LastModificationDate, Is.EqualTo(now.AddMinutes(1)));
        }
    }
}

