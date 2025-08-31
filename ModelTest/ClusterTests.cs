using System;
using System.Collections.Generic;
using NUnit.Framework;
using NORCE.Drilling.Cluster.Model;

namespace NORCE.Drilling.Cluster.ModelTest
{
    [TestFixture]
    public class ClusterTests
    {
        [Test]
        public void Cluster_Defaults_AreNullOrFalse()
        {
            var cluster = new NORCE.Drilling.Cluster.Model.Cluster();

            Assert.That(cluster.MetaInfo, Is.Null);
            Assert.That(cluster.Name, Is.Null);
            Assert.That(cluster.Description, Is.Null);
            Assert.That(cluster.CreationDate, Is.Null);
            Assert.That(cluster.LastModificationDate, Is.Null);
            Assert.That(cluster.FieldID, Is.Null);
            Assert.That(cluster.IsSingleWell, Is.False);

            // Gaussian properties default to null
            Assert.That(cluster.ReferenceLatitude, Is.Null);
            Assert.That(cluster.ReferenceLongitude, Is.Null);
            Assert.That(cluster.GroundMudLineDepth, Is.Null);
            Assert.That(cluster.TopWaterDepth, Is.Null);

            // Slots dictionary is null by default
            Assert.That(cluster.Slots, Is.Null);
        }

        [Test]
        public void Cluster_SetBasicProperties_PersistsValues()
        {
            var cluster = new NORCE.Drilling.Cluster.Model.Cluster();
            var now = DateTimeOffset.UtcNow;
            var id = Guid.NewGuid();

            cluster.Name = "Test Cluster";
            cluster.Description = "Description";
            cluster.CreationDate = now;
            cluster.LastModificationDate = now.AddMinutes(5);
            cluster.FieldID = id;
            cluster.IsSingleWell = true;

            // MetaInfo remains null unless provided externally
            Assert.That(cluster.MetaInfo, Is.Null);
            Assert.That(cluster.Name, Is.EqualTo("Test Cluster"));
            Assert.That(cluster.Description, Is.EqualTo("Description"));
            Assert.That(cluster.CreationDate, Is.EqualTo(now));
            Assert.That(cluster.LastModificationDate, Is.EqualTo(now.AddMinutes(5)));
            Assert.That(cluster.FieldID, Is.EqualTo(id));
            Assert.That(cluster.IsSingleWell, Is.True);
        }

        [Test]
        public void Cluster_SlotsDictionary_AddAndRetrieve()
        {
            var cluster = new NORCE.Drilling.Cluster.Model.Cluster
            {
                Slots = new Dictionary<Guid, Slot>()
            };

            var slot = new Slot { ID = Guid.NewGuid(), Name = "S1" };
            cluster.Slots![slot.ID] = slot;

            Assert.That(cluster.Slots.ContainsKey(slot.ID), Is.True);
            Assert.That(cluster.Slots[slot.ID], Is.SameAs(slot));
            Assert.That(cluster.Slots[slot.ID].Name, Is.EqualTo("S1"));
        }
    }
}
