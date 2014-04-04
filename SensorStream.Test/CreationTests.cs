using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SensorStream.Test
{
    [TestClass]
    public class CreationTests
    {
        int delay = 200;
        public SSConnection getConnection()
        {
            Thread.Sleep(delay);
            return new SSConnection("http://dodeca.coas.oregonstate.edu");
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void CreateDevice_WithInvalidDevice()
        {
            Device newDev = new Device() { DeviceName = "", Description = ".NET library test", UserName = "Wiggins" };
            SSConnection conn = getConnection();
            conn.CreateDevice(newDev);
        }

        [TestMethod]
        public void CreateDevice_WithValidDevice()
        {
            Device newDev = CreateTestDevice();
            DeleteDevice_AfterInsert(newDev);
        }

        public Device CreateTestDevice()
        {
            Device newDev = new Device() { DeviceName = "tester", Description = ".NET library test", UserName = "Wiggins" };
            SSConnection conn = getConnection();
            Device retDevice = conn.CreateDevice(newDev);
            Assert.AreEqual(retDevice.DeviceName, "tester");
            Assert.AreEqual(retDevice.Description, ".NET library test");
            Assert.AreEqual(retDevice.UserName, "Wiggins");
            Assert.IsFalse(String.IsNullOrEmpty(retDevice.guid.ToString()));
            Assert.IsFalse(String.IsNullOrEmpty(retDevice.Created.ToString()));
            Assert.IsFalse(String.IsNullOrEmpty(retDevice.LatestIP));
            Thread.Sleep(delay);
            return retDevice;
        }

        public void DeleteDevice_AfterInsert(Device dev)
        {
            SSConnection conn = getConnection();
            Assert.IsTrue(conn.DeleteDevice(dev));
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void DeleteDevice_BeforeInsert()
        {
            Device newDev = new Device() { DeviceName = "tester", Description = ".NET library test", UserName = "Wiggins" };
            SSConnection conn = getConnection();
            conn.DeleteDevice(newDev);
        }

        [TestMethod]
        public void CreateSimpleStream_WithValidStuff()
        {
            Device newDev = CreateTestDevice();
            DeviceStream testStream = CreateTestSimpleStream(newDev);
            //delete the device
            DeleteDevice_AfterInsert(newDev);
        }

        public DeviceStream CreateTestSimpleStream(Device newDev)
        {
            SSConnection conn = getConnection();
            //create a simple stream
            DeviceStream newDS = new DeviceStream() { Description = "some crazy stream", Units = "nelsi/inch", Type = "int", Name = "Nelsi per inch of force" };
            DeviceStream retStream = conn.CreateStream(newDev, newDS);
            Assert.IsFalse(retStream.StreamID == Guid.Empty.ToString());
            Assert.AreEqual(retStream.Name, "Nelsi per inch of force");
            Thread.Sleep(50);
            return retStream;
        }

        [TestMethod]
        public void DeleteStream_WithValidStuff()
        {
            Device newDev = CreateTestDevice();
            DeviceStream testStream = CreateTestSimpleStream(newDev);

            //delete that stream
            SSConnection conn = getConnection();
            conn.DeleteStream(newDev, testStream);
            Thread.Sleep(delay);

            //delete the device
            DeleteDevice_AfterInsert(newDev);
        }

        [TestMethod]
        public void DeleteDeviceStreams_WithValidStuff()
        {
            Device newDev = CreateTestDevice();
            DeviceStream testStream = CreateTestSimpleStream(newDev);

            SSConnection conn = getConnection();
            conn.DeleteDeviceStreams(newDev);
            Thread.Sleep(delay);
            //delete the device
            DeleteDevice_AfterInsert(newDev);
        }

        [TestMethod]
        public void CreateComplexStream_Valid()
        {
            Device newDev = CreateTestDevice();

            //create a complex stream
            DeviceStream testStream = CreateTestComplexStream(newDev);

            //delete the complex stream
            SSConnection conn = getConnection();
            conn.DeleteDeviceStreams(newDev);
            Thread.Sleep(delay);
            //delete the device
            DeleteDevice_AfterInsert(newDev);
        }

        private DeviceStream CreateTestComplexStream(Device newDev)
        {
            SSConnection conn = getConnection();
            DeviceStream newDS = new DeviceStream() { Description = "some crazy stream", Type = "Complex", Name = "Nelsi per inch of force", Streams = new List<ComplexStreamInfo>() { new ComplexStreamInfo() { Name = "somethingcomplex", Type = "int", Units = "snorts per volt" } } };
            DeviceStream retStream = conn.CreateStream(newDev, newDS);
            Assert.IsFalse(retStream.StreamID == Guid.Empty.ToString());
            Assert.AreEqual(retStream.Name, "Nelsi per inch of force");
            Thread.Sleep(delay);
            return retStream;
        }

        private bool AddSimpleData(Device dev, DeviceStream str)
        {
            SSConnection conn = getConnection();
            Data d = new Data() { StreamID = str.StreamID, Time = SSConnection.ConvertDateTimeToString(DateTime.Now), Value = "1337" };
            return conn.SendData(dev, d);
        }

        private bool AddComplexData(Device dev, DeviceStream str)
        {
            SSConnection conn = getConnection();
            Data d = new Data() { StreamID = str.StreamID, Time = SSConnection.ConvertDateTimeToString(DateTime.Now), Values = new Dictionary<string, string>() };
            d.Values.Add("somethingcomplex", "1337");
            return conn.SendData(dev, d);
        }

        [TestMethod]
        public void CreateComplexData_Valid()
        {
            Device newDev = CreateTestDevice();

            //create a complex stream
            DeviceStream testStream = CreateTestComplexStream(newDev);

            //add complex data
            Assert.IsTrue(AddComplexData(newDev, testStream));

            //delete the device
            DeleteDevice_AfterInsert(newDev);
        }

        [TestMethod]
        public void CreateSimpleData_Valid()
        {
            Device newDev = CreateTestDevice();
            DeviceStream testStream = CreateTestSimpleStream(newDev);
            //add simple data
            Assert.IsTrue(AddSimpleData(newDev, testStream));

            DeleteDevice_AfterInsert(newDev);
        }

        [TestMethod]
        public void GetDevices_Valid()
        {
            SSConnection conn = getConnection();
            DeviceList devices = conn.GetDevices();
            Assert.IsTrue(devices.Devices.Count > 0);
        }

        [TestMethod]
        public void GetStreams_Valid()
        {
            Device newDev = CreateTestDevice();
            DeviceStream testStream = CreateTestSimpleStream(newDev);

            //delete that stream
            SSConnection conn = getConnection();
            Device dev = conn.GetStreamsFromDeviceID(newDev);
            CheckStreamValid(dev.Streams[0]);
            dev = conn.GetStreamFromStreamID(testStream);
            CheckStreamValid(dev.Streams[0]);
            dev = conn.GetStreamsFromUserAndDeviceNames(newDev);
            CheckStreamValid(dev.Streams[0]);
            //delete the device
            DeleteDevice_AfterInsert(newDev);
        }

        private void CheckStreamValid(DeviceStream deviceStream)
        {
            Assert.AreEqual(deviceStream.Name, "Nelsi per inch of force");
        }

        [TestMethod]
        public void GetSimpleData_Valid()
        {
            Device newDev = CreateTestDevice();
            DeviceStream testStream = CreateTestSimpleStream(newDev);
            //add simple data
            Assert.IsTrue(AddSimpleData(newDev, testStream));

            SSConnection conn = getConnection();
            DataGetResponse d = conn.GetData(testStream);
            Assert.IsTrue(d.Stream.Data.Count == 1);
            d = conn.GetData(testStream, true);
            Assert.IsTrue(d.Stream.Data.Count == 1);
            Assert.IsTrue(String.IsNullOrEmpty(d.Stream.Data[0].Value));

            DeleteDevice_AfterInsert(newDev);
        }

        [TestMethod]
        public void GetComplexData_Valid()
        {
            Device newDev = CreateTestDevice();

            //create a complex stream
            DeviceStream testStream = CreateTestComplexStream(newDev);

            //add complex data
            Assert.IsTrue(AddComplexData(newDev, testStream));

            //Get the complex data
            SSConnection conn = getConnection();
            DataGetResponse d = conn.GetData(testStream);
            Assert.IsTrue(d.Stream.Data.Count == 1);
            d = conn.GetData(testStream, true);
            Assert.IsTrue(d.Stream.Data.Count == 1);
            Assert.IsTrue(d.Stream.Data[0].Values == null);

            //delete the device
            DeleteDevice_AfterInsert(newDev);
        }

        [TestMethod]
        public void GetStatistics_Valid()
        {
            Device newDev = CreateTestDevice();
            DeviceStream testStream = CreateTestSimpleStream(newDev);
            //add simple data
            Assert.IsTrue(AddSimpleData(newDev, testStream));

            SSConnection conn = getConnection();
            NumericalStatistics stats = conn.GetStatistics(testStream);
            //Assert.IsTrue();
            //d = conn.GetData(testStream, true);
            //Assert.IsTrue(d.Stream.Data.Count == 1);
            //Assert.IsTrue(String.IsNullOrEmpty(d.Stream.Data[0].Value));

            DeleteDevice_AfterInsert(newDev);
        }

        [TestMethod]
        public void GetAudio_Valid()
        {
            SSConnection conn = getConnection();
            List<Audio> aud = conn.SearchAudio("Coffee");
            Assert.IsTrue(aud.Count != 0);
        }

        [TestMethod]
        public void SubscribeTest()
        {
            SSConnection conn = getConnection();
            Device newDev = CreateTestDevice();
            DeviceStream testStream = CreateTestSimpleStream(newDev);

            //subscribe
            bool connected = conn.startConnection("http://dodeca.coas.oregonstate.edu/");
            conn.dataRecieved += conn_dataRecieved;
            conn.Subscribe(testStream.StreamID);

            //add simple data
            Assert.IsTrue(AddSimpleData(newDev, testStream));

            //make sure we got the data from signalr
            int i = 0;
            while (i < 10 && !dataRecieved)
            {
                Thread.Sleep(1000);
                Assert.IsTrue(AddSimpleData(newDev, testStream));
                i++;
            }
            if (dataRecieved == false)
            {
                DeleteDevice_AfterInsert(newDev);
                throw new Exception("Failed!");
            }
            //unsub
            conn.Unsubscribe(testStream.StreamID);
            Thread.Sleep(1000);

            dataRecieved = false;

            //add data
            Assert.IsTrue(AddSimpleData(newDev, testStream));

            //make sure we didnt get anything
            while (i < 10 && !dataRecieved)
            {
                Assert.IsTrue(AddSimpleData(newDev, testStream));
                Thread.Sleep(500);
                i++;
            }

            if (dataRecieved == true)
            {
                DeleteDevice_AfterInsert(newDev);
                throw new Exception("Failed!");
            }
            //delete device
            DeleteDevice_AfterInsert(newDev);

        }

        static bool dataRecieved = false;
        void conn_dataRecieved(object sender, EventArgs e)
        {
            dataRecieved = true;
        }

    }
}
