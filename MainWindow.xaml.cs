using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks; // Added to prevent the Task errors
using System.Windows;
using System.Windows.Controls;

namespace Part_2
{
    public partial class MainWindow : Window
    {
        private string userName = "";
        private readonly Random random = new Random();
        private readonly string memoryFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "memory.txt");
        private string currentTopic = "";

        // Helper objects
        private readonly DatabaseHelper dbHelper = new DatabaseHelper();
        private List<QuizQuestion> quizQuestions = new List<QuizQuestion>();
        private int currentQuestionIndex = 0;
        private int quizScore = 0;

        // Managers
        private readonly ActivityLogManager logManager;
        private readonly TaskManager taskManager;

        // Chatbot Responses
        private readonly Dictionary<string, string[]> cyberResponses = new Dictionary<string, string[]>()
        {
            {
                "phishing",
                new string[]
                {
                    "Phishing is a trick where someone pretends to be a trusted friend or company to get you to share private information, like passwords.",
                    "Phishing usually happens through suspicious emails, text messages, or fake websites.",
                    "An attempt to break into your account by tricking you into clicking a link or downloading something unsafe."
                }
            },
            {
                "malware",
                new string[]
                {
                    "Malware is short for 'malicious software'. It's any program designed to sneak onto your computer and cause trouble without you knowing.",
                    "This includes things like viruses, spyware, and ransomware that can slow down your system or steal your files.",
                    "Software designed by hackers to spy on you, steal your personal data, or lock you out of your computer."
                }
            },
            {
                "password",
                new string[]
                {
                    "A password is like a digital key to your accounts. Keeping it strong and secret is your first line of defense.",
                    "Try using a long passphrase made of several random words, numbers, and symbols to make it hard to guess.",
                    "Using a secure password manager is a great way to keep track of strong, unique passwords without having to memorize them all."
                }
            },
            {
                "scam",
                new string[]
                {
                    "An online scam is a trick used to get your money, personal details, or control of your computer under false pretenses.",
                    "Common ones include fake tech support alerts, urgent prize notifications, or people pretending to be from your bank.",
                    "Always take a second to verify who you are talking to before sending money or giving away personal information."
                }
            },
            {
                "privacy",
                new string[]
                {
                    "Privacy is about being in control of who gets to see your personal information and online activity.",
                    "It's a good habit to check your privacy settings on social media to control who can see your posts and personal details.",
                    "Guarding your digital footprint keeps you safe from unwanted attention and identity theft."
                }
            }
        };

        private readonly Dictionary<string, string[]> topicKeyWord = new Dictionary<string, string[]>()
        {
            { "phishing", new string[]{ "fraudulent emails", "suspicious messages", "harmful links", "phishing" } },
            { "malware", new string[]{ "virus", "spyware", "ransomware", "trojan horse", "worm" } },
            { "password", new string[]{ "graphical password", "otp", "passphrase", "password" } },
            { "scam", new string[]{ "online scams", "impersonation", "investment scams", "scam" } },
            { "privacy", new string[]{ "privacy setting", "personal data", "privacies", "privacy" } }
        };

        public MainWindow()
        {
            InitializeComponent();
            InitializeQuizQuestions();

            // Initialize managers
            logManager = new ActivityLogManager(dbHelper);
            taskManager = new TaskManager(dbHelper, logManager);
        }

        // Start button
        public void Start_Click(object sender, RoutedEventArgs e)
        {
            WelcomeGrid.Visibility = Visibility.Collapsed;
            NameGrid.Visibility = Visibility.Visible;

            try
            {
                Greeting greeting = new Greeting();
                greeting.PlayVoiceGreeting();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Audio failed: {ex.Message}");
            }
        }

        // Submit name (Asynchronous to prevent UI freezing)
        public async void Submit_Click(object sender, RoutedEventArgs e)
        {
            string name = NameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorText.Text = "Please enter your name";
                MessageBox.Show("Please type your name so I know who I'm chatting with!", "Let's Get Started", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Regex.IsMatch(name, @"^[a-zA-Z\s]+$"))
            {
                ErrorText.Text = "Letters only, please";
                MessageBox.Show("Please enter a valid name using only letters.", "Check Your Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            userName = name;
            ErrorText.Text = "";

            logManager.UserName = userName;
            taskManager.UserName = userName;

            // UI transitions instantly so the app feels fast
            NameGrid.Visibility = Visibility.Collapsed;
            MainTabControl.Visibility = Visibility.Visible;

            LogoText.Text = AsciiArt.ShieldLogo;

            // Display a loading indicator in chat
            ChatListBox.AppendText("Chatbot: Establishing secure connection to database. Please standby...\n\n");

            // Load saved topic (reads quickly from text file)
            Dictionary<string, string> userMemory = LoadUserMemory();
            string welcomeMessage;
            if (userMemory.ContainsKey(userName))
            {
                string savedTopic = userMemory[userName];
                welcomeMessage = $"Chatbot: Welcome back, {userName}! I remember your favorite cybersecurity topic is: {savedTopic}.\n\n";
            }
            else
            {
                welcomeMessage = $"Chatbot: Welcome to AI assistance, {userName}!\nWhat is your favorite cybersecurity topic?\n\n";
            }

            // Run database tasks in the background
            await TryDatabaseActionAsync(async () => {
                List<TaskItem> tasks = null;
                List<string> logs = null;

                // This executes off the UI thread (preventing any lag or freeze!)
                await Task.Run(() => {
                    dbHelper.InitializeDatabase();
                    logManager.Log("User logged in");
                    tasks = taskManager.GetTasks();
                    logs = logManager.GetLogs();
                });

                // Bind results safely back to the UI thread
                TaskListView.ItemsSource = tasks;
                LogListBox.ItemsSource = logs;

                // Connection complete, show welcome message
                ChatListBox.AppendText(welcomeMessage);
            }, "Initialize session");
        }

        // Handle database errors gently
        private void TryDatabaseAction(Action action, string actionDescription)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hmm, I had trouble connecting to the database ({actionDescription}). Please make sure your MySQL server is running.\n\nDetails: {ex.Message}",
                                "Connection Issue", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Handle database errors asynchronously
        private async Task TryDatabaseActionAsync(Func<Task> action, string actionDescription)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not perform database action ({actionDescription}). Please ensure your MySQL server is running.\n\nDetails: {ex.Message}",
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Chat functions
        public void Send_Click(object sender, RoutedEventArgs e)
        {
            string rawMessage = InputTextBox.Text;
            string message = rawMessage.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(message))
            {
                ChatListBox.AppendText("Chatbot: I didn't quite catch that. Could you please rephrase?\n");
                InputTextBox.Clear();
                return;
            }

            ChatListBox.AppendText($"{userName}: {rawMessage}\n");

            // Check for log request
            if (message.Contains("activity log") || message.Contains("what have you done") || message.Contains("my history") || message.Contains("recent logs"))
            {
                TryDatabaseAction(() => {
                    var logs = logManager.GetLogs();
                    ChatListBox.AppendText("Chatbot: Here is a quick look at your recent activity:\n");
                    for (int i = 0; i < logs.Count; i++)
                    {
                        ChatListBox.AppendText($" {i + 1}. {logs[i]}\n");
                    }
                    ChatListBox.AppendText("\n");
                    logManager.Log("Requested activity log via chat");
                }, "Retrieve history");

                InputTextBox.Clear();
                RefreshLogs();
                return;
            }

            // Check for a new task using friendly key phrases
            if (message.StartsWith("enter a task") || message.StartsWith("note down") || message.StartsWith("remind me to") || message.StartsWith("add task"))
            {
                ParseAndAddTaskFromChat(message);
                InputTextBox.Clear();
                return;
            }

            // Check for quiz
            if (message.Contains("start quiz") || message.Contains("play quiz") || message.Contains("quiz") || message.Contains("test me"))
            {
                MainTabControl.SelectedIndex = 2; // Switch to Quiz tab
                ChatListBox.AppendText("Chatbot: Let's test your knowledge! I've opened the Quiz tab for you.\n\n");
                InputTextBox.Clear();
                return;
            }

            // Save favorite topic
            if (message.Contains("interested in") || message.Contains("favorite topic is") || message.Contains("like learning about"))
            {
                SaveToFile(message);
                InputTextBox.Clear();
                return;
            }

            string botResponse = chatBotResponse(message);
            ChatListBox.AppendText($"Chatbot: {botResponse} \n\n");
            InputTextBox.Clear();
        }

        private void ParseAndAddTaskFromChat(string text)
        {
            TryDatabaseAction(() => {
                bool success = taskManager.ParseAndAddTaskFromChat(text, out string resultMessage);
                ChatListBox.AppendText($"Chatbot: {resultMessage}\n\n");
                if (success)
                {
                    RefreshTasks();
                    RefreshLogs();
                }
            }, "Add task from Chat");
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
                return $"{GetSentimentSupport(sentiment)} What cybersecurity topic would you like to talk about today? Ask me about phishing, malware, scams, passwords, or privacy!";
            }

            return "I'm not completely sure about that. Try asking me a question about phishing or passwords, or type 'enter a task [details]' to note down an objective!";
        }

        public string DetectTopic(string message)
        {
            foreach (var topic in topicKeyWord)
            {
                if (topic.Value.Any(word => message.Contains(word)))
                    return topic.Key;
            }
            foreach (var topic in cyberResponses)
            {
                if (message.Contains(topic.Key))
                    return topic.Key;
            }
            return "";
        }

        public string BuildResponses(string topic, string sentiment, bool moreInfo)
        {
            if (!cyberResponses.ContainsKey(topic))
                return "I have some general security tips, but nothing specific on that topic just yet.";

            string[] foundResponse = cyberResponses[topic];
            int index = random.Next(foundResponse.Length);
            string response = foundResponse[index];
            string support = GetSentimentSupport(sentiment);

            if (!string.IsNullOrEmpty(support))
                return $"{support}\n\nHere is a helpful tip on that:\n-> {response}";

            return response;
        }

        public string GetSentimentSupport(string sentiment)
        {
            if (sentiment == "worried")
                return $"I understand feeling worried, {userName}. Cyber threats can seem intimidating, but building a few simple habits will keep you very safe.";
            if (sentiment == "frustrated")
                return $"I hear you, {userName}. Security rules can sometimes feel like a hassle. Let's take it one step at a time.";
            if (sentiment == "curious")
                return $"It's awesome that you want to learn more, {userName}! Staying curious is the best way to stay protected.";

            return "";
        }

        public string DetectSentiment(string message)
        {
            if (message.Contains("worried") || message.Contains("anxious") || message.Contains("nervous") || message.Contains("unsure") || message.Contains("afraid"))
                return "worried";
            if (message.Contains("frustrated") || message.Contains("annoyed") || message.Contains("angry") || message.Contains("confused") || message.Contains("stuck"))
                return "frustrated";
            if (message.Contains("curious") || message.Contains("interested") || message.Contains("learn") || message.Contains("wondering"))
                return "curious";

            return "";
        }

        public bool isFollowUp(string message)
        {
            return message.Contains("explain more") || message.Contains("more details") || message.Contains("another tip") || message.Contains("tell me more");
        }

        // Task methods 
        private void RefreshTasks()
        {
            TaskListView.ItemsSource = taskManager.GetTasks();
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            string title = TaskTitleTextBox.Text.Trim();
            string desc = TaskDescTextBox.Text.Trim();
            DateTime? date = TaskDatePicker.SelectedDate;

            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Please type in a title for your task first.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TryDatabaseAction(() => {
                taskManager.AddTask(title, desc, date);

                // Clear the form
                TaskTitleTextBox.Clear();
                TaskDescTextBox.Clear();
                TaskDatePicker.SelectedDate = null;

                RefreshTasks();
                RefreshLogs();
                MessageBox.Show("I've successfully saved that task for you!", "Task Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }, "Save task");
        }

        private void CompleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (TaskListView.SelectedItem is TaskItem selectedTask)
            {
                TryDatabaseAction(() => {
                    taskManager.CompleteTask(selectedTask.Id, selectedTask.Title);
                    RefreshTasks();
                    RefreshLogs();
                }, "Complete task");
            }
            else
            {
                MessageBox.Show("Please select a task from the list to mark as done.", "Choose a Task", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (TaskListView.SelectedItem is TaskItem selectedTask)
            {
                var result = MessageBox.Show($"Are you sure you want to remove the task '{selectedTask.Title}'?", "Remove Task", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    TryDatabaseAction(() => {
                        taskManager.DeleteTask(selectedTask.Id, selectedTask.Title);
                        RefreshTasks();
                        RefreshLogs();
                    }, "Delete task");
                }
            }
            else
            {
                MessageBox.Show("Please select a task from the list to remove.", "Choose a Task", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Quiz methods
        private void InitializeQuizQuestions()
        {
            quizQuestions = new List<QuizQuestion>()
            {
                new QuizQuestion("Which of the following is a sign of a phishing email?", "Generic greeting & urgent threat", "Official company email address", "Correct spelling & grammar", "No links in body", "A"),
                new QuizQuestion("Is it safe to write down your passwords on a post-it note on your monitor?", "True", "False", "", "", "B"),
                new QuizQuestion("What is malware?", "Harmful software designed to exploit systems", "Antivirus software", "A hardware component", "Safe web browsing tools", "A"),
                new QuizQuestion("Two-Factor Authentication (2FA) significantly secures your accounts.", "True", "False", "", "", "A"),
                new QuizQuestion("Which protocols verify that website data is encrypted during transmission?", "HTTPS", "HTTP", "FTP", "SMTP", "A"),
                new QuizQuestion("Social engineering relies on manipulating human psychology rather than technical exploits.", "True", "False", "", "", "A"),
                new QuizQuestion("Ransomware is a type of malware that locks down personal files and demands payment.", "True", "False", "", "", "A"),
                new QuizQuestion("It is safe to use public open Wi-Fi for bank transactions if you do not have cellular data.", "True", "False", "", "", "B"),
                new QuizQuestion("What should you do if you receive an email claiming you won a lottery from an unknown address?", "Delete/Report it as spam", "Click the links immediately", "Provide personal information", "Reply to ask for details", "A"),
                new QuizQuestion("A secure password should contain at least 12 characters, including mix of cases, numbers, and symbols.", "True", "False", "", "", "A"),
                new QuizQuestion("Antivirus software must be updated constantly to protect against newly emerged security signatures.", "True", "False", "", "", "A")
            };
        }

        private void StartQuiz_Click(object sender, RoutedEventArgs e)
        {
            QuizStartPanel.Visibility = Visibility.Collapsed;
            QuizQuestionPanel.Visibility = Visibility.Visible;
            QuizScorePanel.Visibility = Visibility.Collapsed;

            currentQuestionIndex = 0;
            quizScore = 0;

            TryDatabaseAction(() => {
                logManager.Log("Started the security quiz");
                RefreshLogs();
            }, "Log quiz start");

            ShowQuestion();
        }

        private void ShowQuestion()
        {
            QuizFeedbackText.Visibility = Visibility.Collapsed;
            SubmitAnswerButton.Visibility = Visibility.Visible;
            NextQuestionButton.Visibility = Visibility.Collapsed;

            // Clear answers
            OptARadioButton.IsChecked = false;
            OptBRadioButton.IsChecked = false;
            OptCRadioButton.IsChecked = false;
            OptDRadioButton.IsChecked = false;

            var currentQ = quizQuestions[currentQuestionIndex];
            QuizCounterText.Text = $"Question {currentQuestionIndex + 1} of {quizQuestions.Count}";
            QuestionTextBlock.Text = currentQ.QuestionText;

            // Show answer options
            OptARadioButton.Content = currentQ.OptA;
            OptBRadioButton.Content = currentQ.OptB;

            if (string.IsNullOrEmpty(currentQ.OptC))
            {
                OptCRadioButton.Visibility = Visibility.Collapsed;
                OptDRadioButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                OptCRadioButton.Visibility = Visibility.Visible;
                OptDRadioButton.Visibility = Visibility.Visible;
                OptCRadioButton.Content = currentQ.OptC;
                OptDRadioButton.Content = currentQ.OptD;
            }
        }

        private void SubmitAnswer_Click(object sender, RoutedEventArgs e)
        {
            string selected = "";
            if (OptARadioButton.IsChecked == true) selected = "A";
            else if (OptBRadioButton.IsChecked == true) selected = "B";
            else if (OptCRadioButton.IsChecked == true) selected = "C";
            else if (OptDRadioButton.IsChecked == true) selected = "D";

            if (string.IsNullOrEmpty(selected))
            {
                MessageBox.Show("Please select an option to submit your answer.", "Pick an Answer", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var currentQ = quizQuestions[currentQuestionIndex];
            bool isCorrect = (selected == currentQ.CorrectAnswer);

            if (isCorrect)
            {
                quizScore++;
                QuizFeedbackText.Text = "Correct! Great job understanding this concept.";
                QuizFeedbackText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                QuizFeedbackText.Text = $"Not quite! The correct answer was ({currentQ.CorrectAnswer}).";
                QuizFeedbackText.Foreground = System.Windows.Media.Brushes.Red;
            }

            QuizFeedbackText.Visibility = Visibility.Visible;
            SubmitAnswerButton.Visibility = Visibility.Collapsed;
            NextQuestionButton.Visibility = Visibility.Visible;
        }

        private void NextQuestion_Click(object sender, RoutedEventArgs e)
        {
            currentQuestionIndex++;
            if (currentQuestionIndex < quizQuestions.Count)
            {
                ShowQuestion();
            }
            else
            {
                // Finish quiz
                QuizQuestionPanel.Visibility = Visibility.Collapsed;
                QuizScorePanel.Visibility = Visibility.Visible;

                QuizFinalScoreText.Text = $"Your Score: {quizScore} / {quizQuestions.Count}";

                if (quizScore == quizQuestions.Count)
                    QuizConclusionText.Text = "Amazing! You have a perfect grasp of digital security.";
                else if (quizScore >= 7)
                    QuizConclusionText.Text = "Great job! You have a solid awareness of online safety rules.";
                else
                    QuizConclusionText.Text = "A quick review of phishing and passwords will help boost your score next time!";

                TryDatabaseAction(() => {
                    logManager.Log($"Completed the security quiz. Score: {quizScore}/{quizQuestions.Count}");
                    RefreshLogs();
                }, "Log quiz end");
            }
        }

        private void PlayAgain_Click(object sender, RoutedEventArgs e)
        {
            QuizStartPanel.Visibility = Visibility.Visible;
            QuizScorePanel.Visibility = Visibility.Collapsed;
        }

        // Activity log methods
        private void RefreshLogs()
        {
            TryDatabaseAction(() => {
                LogListBox.ItemsSource = logManager.GetLogs();
            },    "Refresh activity log");
        }

        private void ShowMoreLogs_Click(object sender, RoutedEventArgs e)
        {
            TryDatabaseAction(() => {
                LogListBox.ItemsSource = logManager.ShowMoreLogs();
            }, "Load more history logs");
        }

        // Track tab changes
        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl && !string.IsNullOrEmpty(userName))
            {
                TabItem selectedTab = (TabItem)MainTabControl.SelectedItem;
                if (selectedTab != null)
                {
                    TryDatabaseAction(() => {
                        logManager.Log($"Viewed tab: {selectedTab.Header}");
                        RefreshLogs();
                    }, "Log tab navigation");
                }
            }
        }

        // Save user memory
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
                MessageBox.Show($"Could not save your favorite topic to the local text file: {ex.Message}", "File Save Issue", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SaveToFile(string message)
        {
            string topic = "";
            string[] rawPrefixes = { "i am interested in", "interested in", "my favorite topic is", "favorite topic is", "like learning about" };

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
                SaveUserMemory(userName, topic);
                ChatListBox.AppendText($"Chatbot: Noted! I'll remember that you like learning about \"{topic}\"!\n\n");
            }
            else
            {
                ChatListBox.AppendText("Chatbot: I realized you wanted to save a topic, but I couldn't understand it. Try typing: 'My favorite topic is [topic]'\n\n");
            }
        }
    }

    // Quiz question
    public struct QuizQuestion
    {
        public string QuestionText { get; set; }
        public string OptA { get; set; }
        public string OptB { get; set; }
        public string OptC { get; set; }
        public string OptD { get; set; }
        public string CorrectAnswer { get; set; }

        public QuizQuestion(string text, string a, string b, string c, string d, string correct)
        {
            QuestionText = text;
            OptA = a;
            OptB = b;
            OptC = c;
            OptD = d;
            CorrectAnswer = correct;
        }
    }
}