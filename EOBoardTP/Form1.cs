using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json.Linq;
using RotationClass;

namespace EOBoardTP
{
    public partial class Form1 : Form
    {
        IMqttClient mqttClient;
        // string eoBoardMqttTopic = "EC:19:AD:26:DC:2E";
        string eoBoardMqttTopic = "";
        string eoBoardNameString = "ElectronOptics";
        string mqttBrokerAddressString = "192.168.0.11";
        string mqttBrokerAddressStringRaspi = "169.254.0.10";

        System.Globalization.CultureInfo EnglishCulture = new System.Globalization.CultureInfo("en-us");

   

        string[] paramListe = { "RotationMatrixScanXUpperX", "RotationMatrixScanXUpperY", "RotationMatrixScanYUpperX", "RotationMatrixScanYUpperY",
        "RotationMatrixScanXLowerX", "RotationMatrixScanXLowerY", "RotationMatrixScanYLowerX", "RotationMatrixScanYLowerY",
        "ScanGainUpperX",  "ScanGainUpperY",  "ScanGainLowerX", "ScanGainLowerY"
        };

       Rotation RotationUpper= new Rotation();
       Rotation RotationLower= new Rotation();

        public Form1()
        {
            InitializeComponent();

            // This block tries to connect to MQTT and just skips after 500 ms if there is no connection
            IAsyncResult res;
            Action action = () =>
            {
                ConnectToMQTT();
            };
            res = action.BeginInvoke(null, null);
            res.AsyncWaitHandle.WaitOne(500);

            textBoxRotationUpper.KeyDown += new KeyEventHandler(textBoxRotationUpper_KeyDown);
            textBoxRotationLower.KeyDown += new KeyEventHandler(textBoxRotationLower_KeyDown);

         
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        public async void ConnectToMQTT()
        {
            mqttClient = new MqttFactory().CreateMqttClient();
            var clientId = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds().ToString();
            var mqttClientOptions = new MqttClientOptionsBuilder().WithClientId(clientId).WithTcpServer(mqttBrokerAddressStringRaspi).WithTimeout(TimeSpan.FromSeconds(1)).Build();
            try
            {
                await mqttClient.ConnectAsync(mqttClientOptions);
            }
            catch
            {
                mqttClientOptions = new MqttClientOptionsBuilder().WithClientId(clientId).WithTcpServer(mqttBrokerAddressString).WithTimeout(TimeSpan.FromSeconds(1)).Build();
               try
               {
                    await mqttClient.ConnectAsync(mqttClientOptions);
                }
                catch 
                {
                 return;
                }
 
            }
            LogDebug("MQTT client has been found at " + mqttBrokerAddressString);
            await mqttClient.SubscribeAsync("ZeissMicro/" + eoBoardNameString + "/#");
            mqttClient.ApplicationMessageReceivedAsync += OnReceiveMttMessage;

        }

        public void PublishMqttMessage(string name, string message)
        {
            if (eoBoardMqttTopic != "")
            {
                // mqttClient.PublishStringAsync(eoBoardMqttTopic + "/Command/" + name, message, MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce);
                //  mqttClient.PublishStringAsync("ZeissMicro/" + eoBoardNameString + "/" + eoBoardMqttTopic + "/Command/" + name, message, MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce);
                mqttClient.PublishStringAsync(eoBoardMqttTopic + "/Command/" + name, message, MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce);
                if (checkBox_ShowLog.Checked)
                {
                    LogDebug("Sent: " + eoBoardMqttTopic + "/Command/" + name + " " + message);
                }
            }
            else
            {
                LogDebug("No Mqtt EO Board detected");
            }
        }

        public Task OnReceiveMttMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            var payloadString = e.ApplicationMessage.ConvertPayloadToString();
            var topicString = e.ApplicationMessage.Topic;

            // aus payload musst du den Wert rausholen

            dynamic objectWithFields = JObject.Parse(payloadString);

            if (topicString.EndsWith("/RotationMatrixScanXUpperX") && payloadString.Contains("ReportValue"))
            {
                try
                {
                    Console.WriteLine(Convert.ToDouble(objectWithFields.value));
                }
                catch
                {
                    // Fehlermeldung hier bei Bedarf
                    Console.WriteLine("Could not convert to double. Received: " + payloadString);
                }
            }

            if (eoBoardMqttTopic == "" && topicString.Contains("ZeissMicro/" + eoBoardNameString) && topicString.Contains("ConnectionStatus") && payloadString.ToString().Contains("Connected"))
            {
                eoBoardMqttTopic = topicString.Substring(0, topicString.LastIndexOf("/Status"));
                if (checkBox_ShowLog.Checked)
                { 
                LogDebug("Determined Mqtt topic: " + eoBoardMqttTopic);
                }
            }
            if (checkBox_ShowLog.Checked)
            {
                LogDebug("Received: " + topicString + " " + payloadString);
            }
            updateTextBoxes(topicString, payloadString);

            return Task.CompletedTask;
        }

        private void LogDebug(string message)
        {
            richTextBox1.Invoke(new Action(() => richTextBox1.AppendText(message + Environment.NewLine)));
            richTextBox1.Invoke(new Action(() => richTextBox1.Select(richTextBox1.TextLength, 0)));
            richTextBox1.Invoke(new Action(() => richTextBox1.ScrollToCaret()));
        }

        private void updateTextBoxes(string topicString,string payloadString)
        {

            dynamic objectWithFields = JObject.Parse(payloadString);

            foreach (string paramListe in paramListe)
            {

                if (topicString.EndsWith("/"+paramListe) && payloadString.Contains("ReportValue"))
                {
                    try
                    {
                        Console.WriteLine(Convert.ToDouble(objectWithFields.value));

                        //  textBoxRotScanXUpperX.Text = objectWithFields.value;
                        switch(paramListe)
                        {
                            case "RotationMatrixScanXUpperX":
                                textBoxRotScanXUpperX.Invoke(new Action(() => textBoxRotScanXUpperX.Text = objectWithFields.value));
                                double upperanglecalc = Math.Acos(Convert.ToDouble(objectWithFields.value)/90)*180/Math.PI;
                                textBox_UpperAngleCalc.Invoke(new Action(() => textBox_UpperAngleCalc.Text = upperanglecalc.ToString()));
                                break;
                            case "RotationMatrixScanXUpperY":
                                textBoxRotScanXUpperY.Invoke(new Action(() => textBoxRotScanXUpperY.Text = objectWithFields.value));
                                break;
                            case "RotationMatrixScanYUpperX":
                                textBoxRotScanYUpperX.Invoke(new Action(() => textBoxRotScanYUpperX.Text = objectWithFields.value));
                                if (objectWithFields.value < 0)
                                {
                                    textBox_UpperAngleSign.Invoke(new Action(() => textBox_UpperAngleSign.Text = "-"));
                                }
                                else 
                                { 
                                    textBox_UpperAngleSign.Invoke(new Action(() => textBox_UpperAngleSign.Text = " ")); 
                                }
                                break;
                            case "RotationMatrixScanYUpperY":
                                textBoxRotScanYUpperY.Invoke(new Action(() => textBoxRotScanYUpperY.Text = objectWithFields.value));
                                break;
                            case "RotationMatrixScanXLowerX":
                                textBoxRotScanXUpperX.Invoke(new Action(() => textBoxRotScanXLowerX.Text = objectWithFields.value));
                                double loweranglecalc = Math.Acos(Convert.ToDouble(objectWithFields.value) / 90) * 180 / Math.PI;
                                textBox_LowerAngleCalc.Invoke(new Action(() => textBox_LowerAngleCalc.Text = loweranglecalc.ToString()));
                                break;
                            case "RotationMatrixScanXLowerY":
                                textBoxRotScanXUpperY.Invoke(new Action(() => textBoxRotScanXLowerY.Text = objectWithFields.value));
                                break;
                            case "RotationMatrixScanYLowerX":
                                textBoxRotScanYUpperX.Invoke(new Action(() => textBoxRotScanYLowerX.Text = objectWithFields.value));
                                if (objectWithFields.value < 0)
                                {
                                    textBox_LowerAngleSign.Invoke(new Action(() => textBox_LowerAngleSign.Text = "-"));
                                }
                                else
                                {
                                    textBox_LowerAngleSign.Invoke(new Action(() => textBox_LowerAngleSign.Text = " "));
                                }
                                break;
                            case "RotationMatrixScanYLowerY":
                                textBoxRotScanYUpperY.Invoke(new Action(() => textBoxRotScanYLowerY.Text = objectWithFields.value));
                                break;
                            case "ScanGainUpperX":
                                textBox_ScanGainUpperX.Invoke(new Action(() => textBox_ScanGainUpperX.Text = objectWithFields.value));
                                break;
                            case "ScanGainUpperY":
                                textBox_ScanGainUpperY.Invoke(new Action(() => textBox_ScanGainUpperY.Text = objectWithFields.value));
                                break;
                            case "ScanGainLowerX":
                                textBox_ScanGainLowerX.Invoke(new Action(() => textBox_ScanGainLowerX.Text = objectWithFields.value));
                                break;
                            case "ScanGainLowerY":
                                textBox_ScanGainLowerY.Invoke(new Action(() => textBox_ScanGainLowerY.Text = objectWithFields.value));
                                break;
                        }
                    }
                    catch
                    {
                        // Fehlermeldung hier bei Bedarf
                        Console.WriteLine("Could not extract value from: " + payloadString);
                    }
                }
            }
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (eoBoardMqttTopic != "")
            {
                mqttClient.DisconnectAsync();
                mqttClient.Dispose();
            }
        }

        private void RotationSliderUpperScan_Scroll(object sender, ScrollEventArgs e)
        {
            SetRotation("Upper", RotationSliderUpperScan.Value);

            textBoxRotationUpper.Text = RotationSliderUpperScan.Value.ToString();
        }

        private void RotationSliderLowerScan_Scroll(object sender, ScrollEventArgs e)
        {
            SetRotation("Lower", RotationSliderLowerScan.Value);

            textBoxRotationLower.Text = RotationSliderLowerScan.Value.ToString();
        }

        private void button_ClearLog_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }

        private void textBoxRotationUpper_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                if (!string.IsNullOrEmpty(textBoxRotationUpper.Text))
                {
                    RotationSliderUpperScan.Value = Convert.ToInt32(textBoxRotationUpper.Text);
                    SetRotation("Upper", RotationSliderUpperScan.Value);
                }
            }
        }
        private void textBoxRotationLower_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                if (!string.IsNullOrEmpty(textBoxRotationLower.Text))
                {
                    RotationSliderLowerScan.Value = Convert.ToInt32(textBoxRotationLower.Text);
                    SetRotation("Lower", RotationSliderLowerScan.Value);
                }
            }
        }

        private void SetRotation(string location, int ang)
        {
           
        }

        private void SetScanGain(string location, double scangain)
        {/*
            double ScanGainUpperX = scangain;
            double ScanGainUpperY = scangain;
            double ScanGainLowerX = scangain;
            double ScanGainLowerY = scangain;

            if (location == "Upper")
            {
                PublishMqttMessage(paramListe[8], "{\"messageType\":\"SetValue\",\"name\":\"" + paramListe[8] + "\", \"value\":" + Convert.ToString(ScanGainUpperX, EnglishCulture) + "}");
                PublishMqttMessage(paramListe[9], "{\"messageType\":\"SetValue\",\"name\":\"" + paramListe[9] + "\", \"value\":" + Convert.ToString(ScanGainUpperY, EnglishCulture) + "}");
            }
            else if (location == "Lower")
            {
                PublishMqttMessage(paramListe[10], "{\"messageType\":\"SetValue\",\"name\":\"" + paramListe[10] + "\", \"value\":" + Convert.ToString(ScanGainLowerX, EnglishCulture) + "}");
                PublishMqttMessage(paramListe[11], "{\"messageType\":\"SetValue\",\"name\":\"" + paramListe[11] + "\", \"value\":" + Convert.ToString(ScanGainLowerY, EnglishCulture) + "}");
            }*/
        }


        private void hScrollBar_ScanGainUpper_Scroll(object sender, ScrollEventArgs e)
        {
            SetScanGain("Upper", hScrollBar_ScanGainUpper.Value);
        }

        private void hScrollBar_ScanGainLower_Scroll(object sender, ScrollEventArgs e)
        {
            SetScanGain("Lower", hScrollBar_ScanGainLower.Value);
        }

        private void hScrollBar_Combi_Scroll(object sender, ScrollEventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxCoupleFactor.Text))
            {
               textBoxCoupleFactor.Text = "1";
            }

            double combi = Convert.ToDouble(textBoxCoupleFactor.Text);

            hScrollBar_ScanGainUpper.Value = hScrollBar_Combi.Value;
            hScrollBar_ScanGainLower.Value = Convert.ToInt32(hScrollBar_Combi.Value * combi);


            SetScanGain("Upper", hScrollBar_Combi.Value);
            SetScanGain("Lower", hScrollBar_Combi.Value * combi);
        }
    }
}
