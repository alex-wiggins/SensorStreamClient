using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNet.SignalR.Client;
using System.Collections.Concurrent;

namespace SensorStream
{
    public class SSConnection
    {
        public SSConnection(String server, int numPoints = 500)
        {
            serverURL = server;
            dataPointsToRetrieve = numPoints;
        }
        //this is added on to the server URL
        private string baseURL = "/";

        //api's for device/stream/data manipulation
        private string deviceAPI = "device.ashx?";
        private string streamAPI = "stream.ashx?";
        private string dataAPI = "data.ashx?";
        private string statsAPI = "Stats.ashx?";

        //the server URL. i.e. dodeca.coas.oregonstate.edu
        private string serverURL;
        public string ServerURL
        {
            get { return serverURL; }
            set { serverURL = value; }
        }

        //default number of data points to retrieve on a call to getData (making this too large can cause slowness and problems
        private int dataPointsToRetrieve = 500;
        public int DataPointsToRetrieve
        {
            get { return dataPointsToRetrieve; }
            set { dataPointsToRetrieve = value; }
        }

        ConcurrentQueue<SubscriptionData> dataReceivedQueue = new ConcurrentQueue<SubscriptionData>();
        List<Dictionary<String, bool>> streamsSubscribed = new List<Dictionary<string, bool>>();

        public static string ConvertDateTimeToString(DateTime dt){
            return dt.ToString("yyyy-MM-ddTHH:mm:ss.FFF%K");
        }


        /// <summary>
        /// Method to POST data to the SensorStream Web Service
        /// </summary>
        /// <param name="url">the entire URL that the JSON string will be posted to complete with all paramters</param>
        /// <param name="jsonContent">The JSON string containing all the data that must be posted</param>
        /// <param name="key">For POSTS that require Authentication, the key for the device being published to</param>
        /// <returns>The json string returned from the web service</returns>
        public static string POST(string url, string jsonContent, string key = "")
        {
            //create a request and set the Type to POST
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";

            //turn the JSON string into a byte array
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            Byte[] byteArray = encoding.GetBytes(jsonContent);

            //set the request paramters
            request.ContentLength = byteArray.Length;
            request.ContentType = @"application/json";

            //if a key value was passed in, set it in the POST header
            if (!String.IsNullOrEmpty(key))
            {
                request.Headers.Add("key", key);
            }

            //get the request stream and post the content of the JSON string
            using (Stream dataStream = request.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }
            //check for the response
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                //ensure we actually recieved a response
                if (response.ContentLength == 0)
                {
                    throw new Exception("No response recieved from the web service");
                }

                //read in the entire string and return it to the calling function for processing
                string jsonReturn;
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    jsonReturn = reader.ReadToEnd();
                }
                return jsonReturn;
            }
        }

        public static string GET(string url)
        {
            //create a request and set the Type to POST
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = @"application/json";

            //check for the response
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                //ensure we actually recieved a response
                if (response.ContentLength == 0)
                {
                    throw new Exception("No response recieved from the web service");
                }

                //read in the entire string and return it to the calling function for processing
                string jsonReturn;
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    jsonReturn = reader.ReadToEnd();
                }
                return jsonReturn;
            }
        }

        /// <summary>
        /// Created the device that is passed in as a parameter
        /// </summary>
        /// <param name="device">The device to be created, must contain a Device Name, User Name, and Description</param>
        /// <returns>The newly created device including the GUID that will be required for updating the device and device streams</returns>
        public Device CreateDevice(Device device)
        {
            //scrub the device to make sure the user added all the required fields
            if (string.IsNullOrEmpty(device.DeviceName))
            {
                throw new Exception("Device requires a device name");
            }
            if (string.IsNullOrEmpty(device.UserName))
            {
                throw new Exception("Device requires a user name");
            }
            if (string.IsNullOrEmpty(device.Description))
            {
                throw new Exception("Device requires a description");
            }
            //create the POST string for creating a device
            string jsonPOST = JsonConvert.SerializeObject(device);
            //POST the new device to the web service
            string response = POST(serverURL + baseURL + deviceAPI + "create=a", jsonPOST);

            //parse response into the new Device object and return it, Throw exception if there is an Error or no GUID
            Device retDevice = JsonConvert.DeserializeObject<Device>(response);

            //ensure we got a guid, if we didnt, try and see if we recieved an error
            if (retDevice.guid == Guid.Empty)
            {
                //check for an error
                Error er = JsonConvert.DeserializeObject<Error>(response);
                if (string.IsNullOrEmpty(er.error))
                {
                    //we dont appear to have received an error, throw an exception showing the entire response
                    throw new Exception("No guid retrieved during device creation. No error was returned either. Response was: " + response);
                }
                else
                {
                    //we did receive an error, throw it as an exception
                    throw new Exception(er.error);
                }
            }

            //return the new device
            return retDevice;
        }

        /// <summary>
        /// Creates a given stream for the given device
        /// </summary>
        /// <param name="device">The device that you would like to add a stream to</param>
        /// <param name="stream">The stream you will be adding to the given device</param>
        /// <returns>The stream that was actually created with all the fileds populated</returns>
        public DeviceStream CreateStream(Device device, DeviceStream stream)
        {
            //Scrub all the data we need for creating a stream
            if (device.guid == Guid.Empty)
            {
                throw new Exception("Device requires a guid to create a stream");
            }
            if (string.IsNullOrEmpty(stream.Name))
            {
                throw new Exception("The Stream Requires a Name");
            }
            if (string.IsNullOrEmpty(stream.Description))
            {
                throw new Exception("The Stream requires a description");
            }
            if (string.IsNullOrEmpty(stream.Type))
            {
                throw new Exception("The Stream requires a type");
            }
            if (string.IsNullOrEmpty(stream.Units) && stream.Streams.Count == 0)
            {
                throw new Exception("Invalid Stream");
            }
            //POST the simple stream
            string response = POST(serverURL + baseURL + streamAPI + "create=a", JsonConvert.SerializeObject(stream, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }), device.guid.ToString());
            stream = JsonConvert.DeserializeObject<DeviceStream>(response);
            //ensure we got a guid, if we didnt, try and see if we recieved an error
            if (String.IsNullOrEmpty(stream.StreamID.ToString()))
            {
                //check for an error
                Error er = JsonConvert.DeserializeObject<Error>(response);
                if (string.IsNullOrEmpty(er.error))
                {
                    //we dont appear to have received an error, throw an exception showing the entire response
                    throw new Exception("No guid retrieved during stream creation. No error was returned either. Response was: " + response);
                }
                else
                {
                    //we did receive an error, throw it as an exception
                    throw new Exception(er.error);
                }
            }
            return stream;
        }

        /// <summary>
        /// Used to add data to a given device.
        /// </summary>
        /// <param name="device">the device you would like to add data to. Used for the device key(GUID) </param>
        /// <param name="data">the data you would like to add. Either SimpleData or ComplexData</param>
        /// <returns>true if the data added successfully, false if there was a problem</returns>
        public bool SendData(Device device, Data data)
        {
            //check to make sure the device has a valid GUID
            if (device.guid == Guid.Empty)
            {
                throw new Exception("GUID of the device does not exist. Please use a valid device to add data to.");
            }
            //post the data to the web service, since it will validate the data, we will allow that to happen server side.
            string response = POST(serverURL + baseURL + dataAPI + "create=a", JsonConvert.SerializeObject(data), device.guid.ToString());

            //check the response to see if the add was successful
            DataAddResponse dar = JsonConvert.DeserializeObject<DataAddResponse>(response);
            if (dar.Status == "Success")
            {
                return true;
            }
            return false;
        }


        /// <summary>
        /// Used to add data to a given device.
        /// </summary>
        /// <param name="device">the device you would like to add data to. Used for the device key(GUID) </param>
        /// <param name="data">the data you would like to add. Either SimpleData or ComplexData</param>
        /// <returns>true if the data added successfully, false if there was a problem</returns>
        public bool SendData(Device device, Data[] data)
        {
            //check to make sure the device has a valid GUID
            if (device.guid == Guid.Empty)
            {
                throw new Exception("GUID of the device does not exist. Please use a valid device to add data to.");
            }
            //post the data to the web service, since it will validate the data, we will allow that to happen server side.
            string response = POST(serverURL + baseURL + dataAPI + "create=a", JsonConvert.SerializeObject(data), device.guid.ToString());

            //check the response to see if the add was successful
            DataAddResponse dar = JsonConvert.DeserializeObject<DataAddResponse>(response);
            if (dar.Status == "Success")
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Deletes the device from the server.
        /// </summary>
        /// <param name="device">The device (including GUID) that you wish to delete</param>
        /// <returns>True if delete is successful, false otherwise</returns>
        public bool DeleteDevice(Device device)
        {
            //check to make sure a guid was given with the device
            if (device.guid == Guid.Empty)
            {
                throw new Exception("GUID of the device does not exist. Please use a valid device to delete it.");
            }
            //URL format: server/base/deviceAPI + delete=device
            if (POST(serverURL + baseURL + deviceAPI + "delete=device", "", device.guid.ToString()) != "{ \"status\" : \"succeess\" }")
                return false;
            else
                return true;
        }

        /// <summary>
        /// Deletes all the streams from a device on the server.
        /// </summary>
        /// <param name="device">The device (including GUID) that you wish to delete the streams from</param>
        /// <returns>True if delete is successful, false otherwise</returns>
        public bool DeleteDeviceStreams(Device device)
        {
            //check to make sure a guid was given with the device
            if (device.guid == Guid.Empty)
            {
                throw new Exception("GUID of the device does not exist. Please use a valid device to delete the streams from.");
            }
            //URL format: server/base/deviceAPI + delete=streams
            if (POST(serverURL + baseURL + deviceAPI + "delete=streams", "", device.guid.ToString()) != "{ \"status\" : \"succeess\" }")
                return false;
            else
                return true;
        }

        /// <summary>
        /// Deletes a stream from a device
        /// </summary>
        /// <param name="device">The device to delete the stream from</param>
        /// <param name="stream">The stream to delete</param>
        /// <returns>True if delete is successful, false otherwise</returns>
        public bool DeleteStream(Device device, DeviceStream stream)
        {
            //check to make sure a guid was given with the device
            if (device.guid == Guid.Empty)
            {
                throw new Exception("GUID of the device does not exist. Please use a valid device to delete the streams from.");
            }
            //check to make sure a stream guid 
            if (string.IsNullOrEmpty(stream.StreamID.ToString()))
            {
                throw new Exception("No streamID in the stream. Please use a valid stream to delete.");
            }
            if (POST(serverURL + baseURL + streamAPI + "delete=" + stream.StreamID, "", device.guid.ToString()) != "{ \"status\" : \"succeess\" }")
                return false;
            else
                return true;
        }

        /// <summary>
        /// Gets a list of devices from the server
        /// </summary>
        /// <returns>an array of devices returned from the server</returns>
        public DeviceList GetDevices()
        {
            string response = GET(serverURL + baseURL + deviceAPI + "getdevices=a");
            
            return JsonConvert.DeserializeObject<DeviceList>(response);
        }

        /// <summary>
        /// Gets the device and the streams of the device using the username and devicename 
        /// </summary>
        /// <param name="device">The device containing the username and devicename of the device you are wanting to retrieve streams for</param>
        /// <returns>a new device with the streams populated</returns>
        public Device GetStreamsFromUserAndDeviceNames(Device device)
        {
            return GetStreamsFromUserAndDeviceNames(device.DeviceName, device.UserName);
        }

        /// <summary>
        /// Gets the device and the streams of the device using the username and devicename
        /// </summary>
        /// <param name="deviceName">The name of the device you would like to get the streams for</param>
        /// <param name="userName">Username for the device you are retrieveing the streams for</param>
        /// <returns>a new device with the streams populated</returns>
        public Device GetStreamsFromUserAndDeviceNames(String deviceName, String userName)
        {
            //check to ensure that the fields were at least given a value
            if (String.IsNullOrEmpty(deviceName))
            {
                throw new Exception("No valid device name");
            }
            if (String.IsNullOrEmpty(userName))
            {
                throw new Exception("No valid user name");
            }

            //call to the web service to retrieve the streams
            String response = GET(serverURL + baseURL + streamAPI + "getstreams=" + deviceName + "&user=" + userName);
            return JsonConvert.DeserializeObject<Device>(response);
        }

        /// <summary>
        /// Gets the streams for a device with a given deviceID
        /// </summary>
        /// <param name="device">The device that contains the guid for the streams you would like to retrieve</param>
        /// <returns>a new device with the streams populated</returns>
        public Device GetStreamsFromDeviceID(Device device)
        {
            if (device.guid == Guid.Empty)
            {
                throw new Exception("Invalid device GUID");
            }
            return GetStreamsFromDeviceID(device.guid.ToString());
        }

        /// <summary>
        /// Gets the streams for a device with a given deviceID
        /// </summary>
        /// <param name="deviceID">The deviceID you would liek to retrieve the streams for</param>
        /// <returns>a new device with the streams populated</returns>
        public Device GetStreamsFromDeviceID(String deviceID)
        {
            //ensure we have something to look for
            if (String.IsNullOrEmpty(deviceID))
            {
                throw new Exception("No deviceID given");
            }

            //get response from the server
            String response = GET(serverURL + baseURL + streamAPI + "devid=" + deviceID);
            return JsonConvert.DeserializeObject<Device>(response);
        }

        /// <summary>
        /// Gets the stream information by the streamID
        /// </summary>
        /// <param name="stream">The stream containing a streamID</param>
        /// <returns>a new device with the stream populated</returns>
        public Device GetStreamFromStreamID(DeviceStream stream)
        {
            return GetStreamFromStreamID(stream.StreamID);
        }

        /// <summary>
        /// Gets the stream information by the streamID
        /// </summary>
        /// <param name="streamID">The streamID of the stream you would like to retrieve</param>
        /// <returns>a new device with the stream populated</returns>
        public Device GetStreamFromStreamID(String streamID)
        {
            //check to make sure we have a value for the streamID
            if (String.IsNullOrEmpty(streamID))
            {
                throw new Exception("No streamID given");
            }

            //get and return the response from the server
            String response = GET(serverURL + baseURL + streamAPI + "streamid=" + streamID);
            return JsonConvert.DeserializeObject<Device>(response);
        }

        /// <summary>
        /// Gets data from the given stream
        /// </summary>
        /// <param name="stream">the stream containing the streamid for retrieving the data</param>
        /// <param name="timeonly">true if you only want to retrieve times</param>
        /// <returns>device complete with stream and data</returns>
        public DataGetResponse GetData(DeviceStream stream, bool timeonly = false)
        {
            return GetData(stream.StreamID, dataPointsToRetrieve, timeonly);
        }

        /// <summary>
        /// Gets data from the given stream
        /// </summary>
        /// <param name="streamID">the streamid for retrieving the data</param>
        /// <param name="timeonly">true if you only want to retrieve times</param>
        /// <returns>device complete with stream and data</returns>
        public DataGetResponse GetData(String streamID, bool timeonly = false)
        {
            return GetData(streamID, dataPointsToRetrieve, timeonly);
        }

        /// <summary>
        /// Gets data from the given stream
        /// </summary>
        /// <param name="stream">the stream containing the streamid for retrieving the data</param>
        /// <param name="numberOfPoints">The number of data points to retrieve</param>
        /// <param name="timeonly">true if you only want to retrieve timestamps</param>
        /// <returns>device complete with stream and data</returns>
        public DataGetResponse GetData(DeviceStream stream, int numberOfPoints, bool timeonly = false)
        {
            return GetData(stream.StreamID, numberOfPoints, timeonly);
        }

        /// <summary>
        /// Gets data from the given stream
        /// </summary>
        /// <param name="streamID">the streamid for retrieving the data</param>
        /// <param name="numberOfPoints">The number of data points to retrieve</param>
        /// <param name="timeonly">true if you only want to retrieve timestamps</param>
        /// <returns>device complete with stream and data</returns>
        public DataGetResponse GetData(String streamID, int numberOfPoints, bool timeonly = false)
        {
            //check to make sure we have a valid streamID
            if (string.IsNullOrEmpty(streamID))
            {
                throw new Exception("StreamID was not valid");
            }

            //get the response from the server and deserialize
            String response;
            if (timeonly)
                response = GET(serverURL + baseURL + dataAPI + "getdata=" + streamID + "&count=" + numberOfPoints + "&timeonly=y");
            else
                response = GET(serverURL + baseURL + dataAPI + "getdata=" + streamID + "&count=" + numberOfPoints);

            return JsonConvert.DeserializeObject<DataGetResponse>(response);
        }

        /// <summary>
        /// Gets data after a specified date
        /// </summary>
        /// <param name="stream">The stream containg the streamID</param>
        /// <param name="startDate">the date after which to get the data points</param>
        /// <param name="timeonly">true if you only want times</param>
        /// <returns>a device with the requested data</returns>
        public DataGetResponse GetDataAfter(DeviceStream stream, DateTime startDate, bool timeonly = false)
        {
            return GetDataAfter(stream.StreamID, startDate, dataPointsToRetrieve, timeonly);
        }

        /// <summary>
        /// Gets data after a specified date
        /// </summary>
        /// <param name="streamID">The streamID of the stream to retrieve data</param>
        /// <param name="startDate">the date after which to get the data points</param>
        /// <param name="timeonly">true if you only want times</param>
        /// <returns>a device with the requested data</returns>
        public DataGetResponse GetDataAfter(String streamID, DateTime startDate, bool timeonly = false)
        {
            return GetDataAfter(streamID, startDate, dataPointsToRetrieve, timeonly);
        }

        /// <summary>
        /// Gets data after a specified date
        /// </summary>
        /// <param name="stream">The stream containg the streamID</param>
        /// <param name="startDate">the date after which to get the data points</param>
        /// <param name="numberOfPoints">the number of data points to retrieve</param>
        /// <param name="timeonly">true if you only want the time</param>
        /// <returns>a device with the requested data</returns>
        public DataGetResponse GetDataAfter(DeviceStream stream, DateTime startDate, int numberOfPoints, bool timeonly = false)
        {
            return GetDataAfter(stream.StreamID, startDate, numberOfPoints, timeonly);
        }

        /// <summary>
        /// Gets data after a specified date
        /// </summary>
        /// <param name="streamID">The streamID of the data</param>
        /// <param name="startDate">the date after which to get the data points</param>
        /// <param name="numberOfPoints">the number of data points to retrieve</param>
        /// <param name="timeonly">true if you only want the time</param>
        /// <returns>a device with the requested data</returns>
        private DataGetResponse GetDataAfter(String streamID, DateTime startDate, int numberOfPoints, bool timeonly = false)
        {
            //check to make sure we have a valid streamID
            if (string.IsNullOrEmpty(streamID))
            {
                throw new Exception("StreamID was not valid");
            }

            //get the response from the server and deserialize
            String response;
            if (timeonly)
                response = GET(serverURL + baseURL + dataAPI + "getdata=" + streamID + "&count=" + numberOfPoints + "&start=" + startDate.ToString("yyyy-MM-ddTHH:mm:ss.FFF%K") + "&timeonly=y");
            else
                response = GET(serverURL + baseURL + dataAPI + "getdata=" + streamID + "&count=" + numberOfPoints + "&start=" + startDate.ToString("yyyy-MM-ddTHH:mm:ss.FFF%K"));
            return JsonConvert.DeserializeObject<DataGetResponse>(response);
        }

        /// <summary>
        /// Gets data before a given date
        /// </summary>
        /// <param name="stream">the stream containing the streamID</param>
        /// <param name="endDate">the date to get data before</param>
        /// <param name="timeonly">true if you only want times</param>
        /// <returns>device containg the requested data</returns>
        public DataGetResponse GetDataBefore(DeviceStream stream, DateTime endDate, bool timeonly = false)
        {
            return GetDataBefore(stream.StreamID, endDate, dataPointsToRetrieve, timeonly);
        }

        /// <summary>
        /// Gets data before a given date
        /// </summary>
        /// <param name="streamID">The streamID to get data from</param>
        /// <param name="endDate">the date to get data before</param>
        /// <param name="timeonly">true if you only want times</param>
        /// <returns>device containg the requested data</returns>
        public DataGetResponse GetDataBefore(String streamID, DateTime endDate, bool timeonly = false)
        {
            return GetDataBefore(streamID, endDate, dataPointsToRetrieve, timeonly);
        }

        /// <summary>
        /// Gets data before a given date
        /// </summary>
        /// <param name="stream">the stream containing the streamID</param>
        /// <param name="endDate">the date to get data before</param>
        /// <param name="numberOfPoints">the number of points to retrieve</param>
        /// <param name="timeonly">true if you only want times</param>
        /// <returns>device containg the requested data</returns>
        public DataGetResponse GetDataBefore(DeviceStream stream, DateTime endDate, int numberOfPoints, bool timeonly = false)
        {
            return GetDataBefore(stream.StreamID, endDate, numberOfPoints, timeonly);
        }

        /// <summary>
        /// Gets data before a given date
        /// </summary>
        /// <param name="streamID">the streamID to get data from</param>
        /// <param name="endDate">the date to get data before</param>
        /// <param name="numberOfPoints">the number of points to retrieve</param>
        /// <param name="timeonly">true if you only want times</param>
        /// <returns>device containg the requested data</returns>
        private DataGetResponse GetDataBefore(String streamID, DateTime endDate, int numberOfPoints, bool timeonly = false)
        {
            //check to make sure we have a valid streamID
            if (string.IsNullOrEmpty(streamID))
            {
                throw new Exception("StreamID was not valid");
            }

            //get the response from the server and deserialize
            String response;
            if (timeonly)
                response = GET(serverURL + baseURL + dataAPI + "getdata=" + streamID + "&count=" + numberOfPoints + "&end=" + endDate.ToString("yyyy-MM-ddTHH:mm:ss.FFF%K") + "&timeonly=y");
            else
                response = GET(serverURL + baseURL + dataAPI + "getdata=" + streamID + "&count=" + numberOfPoints + "&end=" + endDate.ToString("yyyy-MM-ddTHH:mm:ss.FFF%K"));
            return JsonConvert.DeserializeObject<DataGetResponse>(response);
        }

        /// <summary>
        /// Gets data beteen two given dates
        /// </summary>
        /// <param name="stream">the stream containing the streamID</param>
        /// <param name="startDate">the date to get data before</param>
        /// <param name="endDate">the date to get data before</param>
        /// <param name="timeonly">true if you only want times</param>
        /// <returns>device containg the requested data</returns>
        public DataGetResponse GetDataBetween(DeviceStream stream, DateTime startDate, DateTime endDate, bool timeonly = false)
        {
            return GetDataBetween(stream.StreamID, startDate, endDate, dataPointsToRetrieve, timeonly);
        }

        /// <summary>
        /// Gets data beteen two given dates
        /// </summary>
        /// <param name="streamID">the streamID for the data</param>
        /// <param name="startDate">the date to get data before</param>
        /// <param name="endDate">the date to get data before</param>
        /// <param name="timeonly">true if you only want times</param>
        /// <returns>device containg the requested data</returns>
        public DataGetResponse GetDataBetween(String streamID, DateTime startDate, DateTime endDate, bool timeonly = false)
        {
            return GetDataBetween(streamID, startDate, endDate, dataPointsToRetrieve, timeonly);
        }

        /// <summary>
        /// Gets data beteen two given dates
        /// </summary>
        /// <param name="stream">the stream containing the streamID</param>
        /// <param name="startDate">the date to get data before</param>
        /// <param name="endDate">the date to get data before</param>
        /// <param name="numberOfPoints">The number of points to retrieve</param>
        /// <param name="timeonly">true if you only want times</param>
        /// <returns>device containg the requested data</returns>
        public DataGetResponse GetDataBetween(DeviceStream stream, DateTime startDate, DateTime endDate, int numberOfPoints, bool timeonly = false)
        {
            return GetDataBetween(stream.StreamID, startDate, endDate, numberOfPoints, timeonly);
        }

        /// <summary>
        /// Gets data beteen two given dates
        /// </summary>
        /// <param name="streamID">the streamID for the data</param>
        /// <param name="startDate">the date to get data before</param>
        /// <param name="endDate">the date to get data before</param>
        /// <param name="numberOfPoints">the number of points to retrieve</param>
        /// <param name="timeonly">true if you only want times</param>
        /// <returns>device containg the requested data</returns>
        public DataGetResponse GetDataBetween(String streamID, DateTime startDate, DateTime endDate, int numberOfPoints, bool timeonly = false)
        {
            //check to make sure we have a valid streamID
            if (string.IsNullOrEmpty(streamID))
            {
                throw new Exception("StreamID was not valid");
            }

            //get the response from the server and deserialize
            String response;
            if (timeonly)
                response = GET(serverURL + baseURL + dataAPI + "getdata=" + streamID + "&count=" + numberOfPoints + "&start=" + startDate.ToString("yyyy-MM-ddTHH:mm:ss.FFF%K") + "&end=" + endDate.ToString("yyyy-MM-ddTHH:mm:ss.FFF%K") + "&timeonly");
            else
                response = GET(serverURL + baseURL + dataAPI + "getdata=" + streamID + "&count=" + numberOfPoints + "&start=" + startDate.ToString("yyyy-MM-ddTHH:mm:ss.FFF%K") + "&end=" + endDate.ToString("yyyy-MM-ddTHH:mm:ss.FFF%K"));
            return JsonConvert.DeserializeObject<DataGetResponse>(response);
        }

        /// <summary>
        /// Searches through all audio on the server looking for the given words or phrase
        /// </summary>
        /// <param name="wordOrPhrase"></param>
        /// <returns></returns>
        public List<Audio> SearchAudio(String wordOrPhrase)
        {
            //check to make sure we have a valid search string
            if (string.IsNullOrEmpty(wordOrPhrase))
            {
                throw new Exception("No valid search string given");
            }

            //get the response from the server and deserialize
            String response = GET(serverURL + baseURL + dataAPI + "audio=" + wordOrPhrase);
            return JsonConvert.DeserializeObject<List<Audio>>(response);
        }

        /// <summary>
        /// Retrieves the statistics of a stream
        /// </summary>
        /// <param name="stream">the stream to get stats for</param>
        /// <returns>the stats of a stream</returns>
        public NumericalStatistics GetStatistics(DeviceStream stream)
        {
            return GetStatistics(stream.StreamID);
        }

        /// <summary>
        /// Retrieves the statistics of a stream
        /// </summary>
        /// <param name="streamID">the stream to get the stats for</param>
        /// <returns>the stats of a stream</returns>
        public NumericalStatistics GetStatistics(String streamID)
        {

            //Check for a valid streamID
            if (String.IsNullOrEmpty(streamID))
            {
                throw new Exception("No Valid streamID found");
            }

            //get the response from the server and deserialize
            String response = GET(serverURL + baseURL + statsAPI + "streamID=" + streamID);
            return JsonConvert.DeserializeObject<NumericalStatistics>(response);
        }

        /// <summary>
        /// An even that is raised when a new object is placed into the data queue
        /// </summary>
        public event EventHandler dataRecieved;

        /// <summary>
        /// the connection the the signalR hub
        /// </summary>
        HubConnection dataHubConnection;
        IHubProxy dataHubProxy;

        public class SubscriptionData
        {
            public string StreamID { get; set; }
            public Data StreamData { get; set; }
        }

        /// <summary>
        /// used to make a connection to the signalR hub on the specified serverURL
        /// </summary>
        /// <param name="serverURL">the URL of the signalr hub</param>
        /// <returns>true if successful</returns>
        public bool startConnection(string serverURL)
        {
            //if we had a connection, get rid of it
            if (dataHubConnection != null)
            {
                dataHubConnection.Dispose();
                dataHubConnection = null;
            }
            if (dataHubProxy != null)
            {
                dataHubProxy = null;
            }

            //Create a new connection
            dataHubConnection = new HubConnection(serverURL);
            dataHubProxy = dataHubConnection.CreateHubProxy("dataHub");

            //create a new hadler for when data is recieved
            dataHubProxy.On<String, String>("newData", (stream, data) =>
                {

                    dataReceivedQueue.Enqueue(new SubscriptionData() { StreamID = stream, StreamData = JsonConvert.DeserializeObject<Data>(data) });
                    EventHandler handler = dataRecieved;
                    if (handler != null)
                    {
                        handler(this, EventArgs.Empty);
                    }

                }
            );
            dataHubConnection.Start().Wait();
            return true;
        }

        /// <summary>
        /// used to subscribe with signalR
        /// </summary>
        /// <param name="streamID">the streamID to subscribe to</param>
        public void Subscribe(String streamID)
        {
            if (dataHubProxy == null)
            {
                throw new Exception("No connection to signalR hub created, did you foget to call startConnection?");
            }
            dataHubProxy.Invoke("Subscribe", streamID);
        }

        public void Unsubscribe(String streamID)
        {
            if (dataHubProxy == null)
            {
               throw new Exception("No connection to signalR hub created, did you foget to call startConnection?");
            }
            dataHubProxy.Invoke("UnSubscribe", streamID);
        }
    }
}
