using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace Part_2
{
    public partial class MainWindow : Window
    {
        //store the user's name
        private string userName = "";
        private readonly Random random = new Random();
        private readonly string memoryFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "memory.txt");
        private string currentTopic = "";

        // Store cybersecurity answers for each topic
        private readonly Dictionary<string, string[]> cyberResponses = new Dictionary<string, string[]>()
        {
            {
                //Explainations of phishing
                "phishing",
                new string[]
                {
                    "Phishing is a cyberattack in which an attacker impersonates a trudted erson, organization or service to trick individuals into revealing sensitive information such as passwords, banking details or personal data.",
                    "Social engineering that uses fraudulent emails, messages, websites or phone calls to decieve users into taking actions that benefit the attacker.",
                    "An authorized attempt to gain access to company systems, accounts or confidential information by exploiting employee trust through deceptive communications.",
                    "A fraudlent practice involving the use of communications to obtain confidential information from individuals under false pretenses, often for financial gain or identity theft.",
                    "A scam where criminals pretend to e someone trustworth and persuade people to share private information or click harmful links."
                }
            },
            {
                   //Explainations of malware
                "malware",
                new string[]
                {
                    "Malware is software created to perform malicious actions on a computer system without the user's consent.",
                    "A broad category of harmful programs used to compromise the confidentiality, inergrity or availaility of digital systems and information.",
                    "Executable code or software that exploits vulnerailities, steals data, disrupts operations or provides unauthorised access to attackers .",
                    "A security threat that can damage organizational systems, interrupt business operations or proovides unauthorized access to attackers.",
                    "A harmful computer program that can infect devices, steal information or cause damage."
                }
            },
            {
                   //Explainations of password
                "password",
                new string[]
                {
                    "A password is a confidential word, phrase or comination of characters used to authenticate a user.",
                    "An autentication credential that helps protect systems and data by verifying that a user is authorized to access them.",
                    "A sequence of characters enetered by a user and comapared against stored authentication data to control access to digital resources.",
                    "A password is a security mechanism used to prevent unauthorized access to accounts, networks, applications or devices.",
                    "A secret code that allows you to log in to an account or use a device."
                }
            },
            {
                   //Explainations of scam
                "scam",
                new string[]
                {
                    "A scam is fraudlent scheme intended to trick people for financial or personal gain.",
                    "Is a deceptive practice that persuades victims to provide money, sensitive information or access to assets under false pretenses .",
                    " A malicious attempt often conducted online to manipulate individuals into revealimg information, transferring funds or performing actions that benefit a criminal.",
                    "An act of fraud invoving intentional deception to otain an unlawful advantage or benefit.",
                    "A trick used y someone to cheat another person out of money, information or property."
                }
            },
            {
                   //Explainations of privacy
                "privacy",
                new string[]
                {
                    "Privacy is the state of being free from unwanted observation, intrusion or interference.",
                    "A protected right that allows individuals to control how their personal data, communications and personal life are accessed or used by others.",
                    "The practice of safeguarding personal or sensitive data fromm unauthorized access, use or disclosure.",
                    "Is the ability of users to control what personal information is collected, shared or stored when using digital platforms and services.",
                    "Having control over your personal space, information and activities without others interfering or watching without permission."
                }
            }
        };

        private readonly Dictionary<string, string[]> topicKeyWord = new Dictionary<string, string[]>()
        {
            //store keywords linked to each topic
            { "phishing", new string[]{ "fraudlent emails", "suspicious messages", "harmful links", "phishing" } },
            { "malware", new string[]{ "virus", "spyware", "ransomware", "trojan horse", "worm" } },
            { "password", new string[]{ "graphical password", "OTP", "Passphrase", "Random password" } },
            { "scam", new string[]{ "online scams", "Impersonation", "Investment scams", " Tech support scams" } },
            { "privacy", new string[]{ "privacy setting", "personal data", "privacies", "privacy" } }
        };

        public MainWindow()
        {
            InitializeComponent();
        }
        // start button click event
        public void Start_Click(object sender, RoutedEventArgs e)
        {
            WelcomeGrid.Visibility = Visibility.Collapsed;
            NameGrid.Visibility = Visibility.Visible;

            // Play voice greeting
            try
            {
                Greeting greeting = new Greeting();
                greeting.PlayVoiceGreeting();
            }
            catch (Exception ex)
            {
                //show error if audio fails to play
                System.Diagnostics.Debug.WriteLine($"Audio failed to play: {ex.Message}");
            }
        }

        public void Submit_Click(object sender, RoutedEventArgs e)
        {
            string name = NameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorText.Text = "Name cannot be empty";
                MessageBox.Show("Please enter a username to proceed.", "Validation Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Regex.IsMatch(name, @"^[a-zA-Z]+$"))
            {
                //check if the name contains only letters
                ErrorText.Text = "Enter a valid name";
                MessageBox.Show("Please enter a valid alphabetic name (letters only).", "Validation Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            userName = name;
            ErrorText.Text = "";

            NameGrid.Visibility = Visibility.Collapsed;
            ChatGrid.Visibility = Visibility.Visible;
            // welcome message
            MessageBox.Show($"Welcome, {userName}!", "Session Loaded", MessageBoxButton.OK, MessageBoxImage.Information);

            //show ASCII logo
            LogoText.Text = AsciiArt.ShieldLogo;

            Dictionary<string, string> userMemory = LoadUserMemory();

            if (userMemory.ContainsKey(userName))
            {
                //check if user already exists
                string savedTopic = userMemory[userName];
                ChatListBox.AppendText($"Chatbot: Welcome back, {userName}! It's great to see you again.\n");
                ChatListBox.AppendText($"Chatbot: I remember your favorite cybersecurity topic is: {savedTopic}.\n\n");
            }
            else
            {
                //Message for new user
                ChatListBox.AppendText($"Chatbot: Welcome to AI assistance, {userName}!\n");
                ChatListBox.AppendText($"Chatbot: Since you are a new user, what is your favorite cybersecurity topic?\n\n");
                //Ask user to save favorite topic
                MessageBoxResult askTopicResult = MessageBox.Show(
                    "Would you like to register your favorite cybersecurity topic now?",
                    "Setup Preference",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (askTopicResult == MessageBoxResult.Yes)
                {
                    ChatListBox.AppendText($"Chatbot: Great! Type: \"My favorite topic is [topic]\" to save it.\n\n");
                }
            }
        }

        public void Send_Click(object sender, RoutedEventArgs e)
        {
            string rawMessage = InputTextBox.Text;
            string message = rawMessage.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(message))
            {
                ChatListBox.AppendText("Chatbot: I’m not sure I understand. Can you try rephrasing?\n");
                InputTextBox.Clear();
                return;
            }

            ChatListBox.AppendText($"{userName}: {rawMessage}\n");

            if (message.Contains("interested in") || message.Contains("favorite topic is"))
            {
                SaveToFile(message);
                InputTextBox.Clear();
                return;
            }
            else if (message.Contains("favorite topic"))
            {
                Dictionary<string, string> userMemory = LoadUserMemory();
                if (userMemory.ContainsKey(userName))
                {
                    string savedTopic = userMemory[userName];
                    ChatListBox.AppendText($"Chatbot: Your favorite topic is: {savedTopic}\n\n");
                }
                else
                {
                    ChatListBox.AppendText("Chatbot: I don't know your favorite topic yet. Say \"My favorite topic is [topic]\" to teach me.\n\n");
                }

                InputTextBox.Clear();
                return;
            }

            string botResponse = chatBotResponse(message);
            ChatListBox.AppendText($"Chatbot: {botResponse} \n\n");

            InputTextBox.Clear();
        }

        public string chatBotResponse(string message)
        {
            string sentiment = DetectSentiment(message);
            bool moreInfo = isFollowUp(message);
            string topic = DetectTopic(message);

            if (string.IsNullOrEmpty(topic) && moreInfo && !string.IsNullOrEmpty(currentTopic))
            {
                topic = currentTopic;
            }

            if (!string.IsNullOrEmpty(topic))
            {
                currentTopic = topic;
                return BuildResponses(topic, sentiment, moreInfo);
            }

            if (!string.IsNullOrEmpty(sentiment))
            {
                return $"{GetSentimentSupport(sentiment)}, Tell me which cybersecurity topic is bothering you, such as phishing, malware, scams, passwords, or privacy, and I will assist you.";
            }

            return "I’m not sure I understand. Can you try rephrasing? I am specifically trained to help you with cybersecurity questions.";
        }
        //detect cybersecurity topic
        public string DetectTopic(string message)
        {
            foreach (var topic in topicKeyWord)
            {
                if (topic.Value.Any(word => message.Contains(word)))
                {
                    return topic.Key;
                }
            }
            foreach (var topic in cyberResponses)
            {
                if (message.Contains(topic.Key))
                {
                    return topic.Key;
                }
            }
            return "";
        }
        //build chatbot response
        public string BuildResponses(string topic, string sentiment, bool moreInfo)
        {
            if (!cyberResponses.ContainsKey(topic))
            {
                //check if the topic exists
                return "I have some general safety tips, but nothing specific on that topic yet.";
            }

            string[] foundResponse = cyberResponses[topic];
            int index = random.Next(foundResponse.Length);
            string response = foundResponse[index];
            string support = GetSentimentSupport(sentiment);

            if (!string.IsNullOrEmpty(support))
            {
                return $"{support}\nHere is some guidance on this topic:\n-> {response}";
            }

            return response;
        }
        //support user emotions
        public string GetSentimentSupport(string sentiment)
        {
            if (sentiment == "worried")
            {
                return $"Hey {userName}, it's completely understandable to feel worried. Cybersecurity threats can seem overwhelming, but a few careful habits can protect you.";
            }

            if (sentiment == "frustrated")
            {
                return $"Hey {userName}, I know this can feel frustrating. Let's slow down and focus on one practical step at a time.";
            }

            if (sentiment == "curious")
            {
                return $"It's fantastic that you are curious about digital safety, {userName}! Proactive learning is your best defense.";
            }

            return "";
        }
        // detect user emotions
        public string DetectSentiment(string message)
        {
            if (message.Contains("worried") ||
                message.Contains("anxious") ||
                message.Contains("nervous") ||
                message.Contains("unsure") ||
                message.Contains("afraid"))
            {
                return "worried";
            }

            if (message.Contains("frustrated") ||
                message.Contains("annoyed") ||
                message.Contains("angry") ||
                message.Contains("confused") ||
                message.Contains("stuck"))
            {
                return "frustrated";
            }

            if (message.Contains("curious") ||
                message.Contains("interested") ||
                message.Contains("learn") ||
                message.Contains("study") ||
                message.Contains("wondering"))
            {
                return "curious";
            }

            return "";
        }

        public bool isFollowUp(string message)
        {
            //check if user wants more information
            return message.Contains("explain more") ||
                message.Contains("more details") ||
                message.Contains("another tip") ||
                message.Contains("tell me more") ||
                message.Contains("i did not understand");
        }

        private Dictionary<string, string> LoadUserMemory()
        {
            var memory = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(memoryFile))
            {
                try
                {
                    string[] lines = File.ReadAllLines(memoryFile);
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            memory[parts[0].Trim()] = parts[1].Trim();
                        }
                    }
                }
                catch (IOException ioEx)
                {
                    System.Diagnostics.Debug.WriteLine($"File read error: {ioEx.Message}");
                }
            }
            return memory;
        }

        private void SaveUserMemory(string username, string topic)
        {
            Dictionary<string, string> memory = LoadUserMemory();
            memory[username] = topic;

            List<string> lines = new List<string>();
            foreach (var kvp in memory)
            {
                lines.Add($"{kvp.Key}={kvp.Value}");
            }

            try
            {
                File.WriteAllLines(memoryFile, lines);
            }
            catch (Exception ex)
            {
                //show save error
                MessageBox.Show($"Failed to save favorite topic: {ex.Message}", "File Write Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        //save topic from chat
        public void SaveToFile(string message)
        {
            string topic = "";
            string[] rawPrefixes = { "i am interested in", "interested in", "my favorite topic is", "favorite topic is" };

            foreach (var prefix in rawPrefixes)
            {
                if (message.Contains(prefix))
                {
                    int index = message.IndexOf(prefix);
                    topic = message.Substring(index + prefix.Length).Trim();
                    break;
                }
            }

            if (!string.IsNullOrEmpty(topic))
            {
                //save the topic if it is found
                SaveUserMemory(userName, topic);
                ChatListBox.AppendText($"Chatbot: I will remember that your favorite topic is \"{topic}\"!\n\n");
            }
            else
            {
                //error if the topic is not found
                ChatListBox.AppendText("Chatbot: I registered that you wanted to save a topic, but couldn't parse it clearly. Please try typing: \"My favorite topic is [topic]\"\n\n");
            }
        }
    }
}.
