// 🚀 **NEURAL REGISTRATION SYSTEM - AI-Powered Form Validation**
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.ComponentModel.DataAnnotations;
using MesTechStok.Core.AI;
using MesTechStok.Desktop.Neural.Components;
using Microsoft.Extensions.Logging;

namespace MesTechStok.Desktop.Neural.Registration
{
    public class NeuralRegistrationWindow : Window
    {
        private readonly IAICore _aiCore;
        private readonly ILogger<NeuralRegistrationWindow> _logger;

        // Neural UI Components
        private NeuralTextBox _usernameTextBox;
        private NeuralTextBox _emailTextBox;
        private NeuralTextBox _passwordTextBox;
        private NeuralTextBox _confirmPasswordTextBox;
        private NeuralButton _registerButton;
        private NeuralButton _cancelButton;
        private NeuralLoadingIndicator _loadingIndicator;

        // Validation States
        private ValidationState _usernameState = ValidationState.Pending;
        private ValidationState _emailState = ValidationState.Pending;
        private ValidationState _passwordState = ValidationState.Pending;
        private ValidationState _confirmPasswordState = ValidationState.Pending;

        public NeuralRegistrationWindow(IAICore aiCore, ILogger<NeuralRegistrationWindow> logger)
        {
            _aiCore = aiCore;
            _logger = logger;
            InitializeNeuralUI();
        }

        private void InitializeNeuralUI()
        {
            // Window Properties
            Title = "🧠 Neural Registration System";
            Width = 500;
            Height = 650;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30));
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;

            // Main Container
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            // Neural Header
            var headerPanel = CreateNeuralHeader();
            Grid.SetRow(headerPanel, 0);
            mainGrid.Children.Add(headerPanel);

            // Form Content
            var contentPanel = CreateFormContent();
            Grid.SetRow(contentPanel, 1);
            mainGrid.Children.Add(contentPanel);

            // Footer Buttons
            var footerPanel = CreateFooterButtons();
            Grid.SetRow(footerPanel, 2);
            mainGrid.Children.Add(footerPanel);

            Content = mainGrid;

            _logger.LogInformation("🧠 Neural Registration System initialized");
        }

        private Panel CreateNeuralHeader()
        {
            var headerPanel = new StackPanel
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 0, 122, 255)),
                Margin = new Thickness(0, 0, 0, 20)
            };

            var titleBlock = new TextBlock
            {
                Text = "🧠 Neural Registration",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 10)
            };

            var subtitleBlock = new TextBlock
            {
                Text = "AI-Powered Intelligent Form Validation",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };

            headerPanel.Children.Add(titleBlock);
            headerPanel.Children.Add(subtitleBlock);

            return headerPanel;
        }

        private Panel CreateFormContent()
        {
            var formPanel = new StackPanel
            {
                Margin = new Thickness(40, 20, 40, 20)
            };

            // Username Field
            var usernameLabel = CreateFieldLabel("Username", "👤");
            _usernameTextBox = new NeuralTextBox
            {
                ValidationContext = "Username",
                EnableAIValidation = true,
                EnableSmartSuggestions = true,
                Margin = new Thickness(0, 5, 0, 15)
            };
            _usernameTextBox.TextChanged += OnUsernameChanged;

            // Email Field
            var emailLabel = CreateFieldLabel("Email Address", "📧");
            _emailTextBox = new NeuralTextBox
            {
                ValidationContext = "Email",
                EnableAIValidation = true,
                EnableSmartSuggestions = true,
                Margin = new Thickness(0, 5, 0, 15)
            };
            _emailTextBox.TextChanged += OnEmailChanged;

            // Password Field
            var passwordLabel = CreateFieldLabel("Password", "🔐");
            _passwordTextBox = new NeuralTextBox
            {
                ValidationContext = "Password",
                EnableAIValidation = true,
                EnableSmartSuggestions = false, // No suggestions for passwords
                Margin = new Thickness(0, 5, 0, 15)
            };
            _passwordTextBox.TextChanged += OnPasswordChanged;

            // Confirm Password Field
            var confirmPasswordLabel = CreateFieldLabel("Confirm Password", "🔒");
            _confirmPasswordTextBox = new NeuralTextBox
            {
                ValidationContext = "ConfirmPassword",
                EnableAIValidation = true,
                EnableSmartSuggestions = false,
                Margin = new Thickness(0, 5, 0, 15)
            };
            _confirmPasswordTextBox.TextChanged += OnConfirmPasswordChanged;

            // Validation Summary
            var validationSummary = CreateValidationSummary();

            // Loading Indicator
            _loadingIndicator = new NeuralLoadingIndicator
            {
                LoadingText = "AI Validating Registration...",
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(0, 10, 0, 0)
            };

            // Add all components
            formPanel.Children.Add(usernameLabel);
            formPanel.Children.Add(_usernameTextBox);
            formPanel.Children.Add(emailLabel);
            formPanel.Children.Add(_emailTextBox);
            formPanel.Children.Add(passwordLabel);
            formPanel.Children.Add(_passwordTextBox);
            formPanel.Children.Add(confirmPasswordLabel);
            formPanel.Children.Add(_confirmPasswordTextBox);
            formPanel.Children.Add(validationSummary);
            formPanel.Children.Add(_loadingIndicator);

            return formPanel;
        }

        private TextBlock CreateFieldLabel(string text, string icon)
        {
            return new TextBlock
            {
                Text = $"{icon} {text}",
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(0, 10, 0, 0)
            };
        }

        private Panel CreateValidationSummary()
        {
            var summaryPanel = new StackPanel
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 45, 45, 45)),
                Margin = new Thickness(0, 20, 0, 0)
            };

            var summaryStack = new StackPanel();

            var titleBlock = new TextBlock
            {
                Text = "🤖 AI Validation Status",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 122, 255)),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var statusList = new StackPanel();
            statusList.Children.Add(CreateValidationStatusItem("Username", _usernameState));
            statusList.Children.Add(CreateValidationStatusItem("Email", _emailState));
            statusList.Children.Add(CreateValidationStatusItem("Password", _passwordState));
            statusList.Children.Add(CreateValidationStatusItem("Confirmation", _confirmPasswordState));

            summaryStack.Children.Add(titleBlock);
            summaryStack.Children.Add(statusList);
            summaryPanel.Children.Add(summaryStack);

            return summaryPanel;
        }

        private UIElement CreateValidationStatusItem(string field, ValidationState state)
        {
            var itemPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 2, 0, 2)
            };

            var icon = state switch
            {
                ValidationState.Valid => "✅",
                ValidationState.Invalid => "❌",
                ValidationState.Warning => "⚠️",
                ValidationState.Pending => "⏳",
                _ => "❓"
            };

            var color = state switch
            {
                ValidationState.Valid => Colors.Green,
                ValidationState.Invalid => Colors.Red,
                ValidationState.Warning => Colors.Orange,
                ValidationState.Pending => Colors.Gray,
                _ => Colors.White
            };

            var iconBlock = new TextBlock
            {
                Text = icon,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var textBlock = new TextBlock
            {
                Text = field,
                Foreground = new SolidColorBrush(color),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };

            itemPanel.Children.Add(iconBlock);
            itemPanel.Children.Add(textBlock);

            return itemPanel;
        }

        private Panel CreateFooterButtons()
        {
            var footerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 20)
            };

            _registerButton = new NeuralButton
            {
                Content = "🚀 Register",
                ActionType = "Register",
                EnableAIValidation = true,
                EnablePredictiveLoading = true,
                Width = 120,
                Height = 40,
                Margin = new Thickness(0, 0, 10, 0),
                IsEnabled = false
            };
            _registerButton.Click += OnRegisterClick;

            _cancelButton = new NeuralButton
            {
                Content = "❌ Cancel",
                ActionType = "Cancel",
                EnableAIValidation = false,
                Width = 120,
                Height = 40,
                Margin = new Thickness(10, 0, 0, 0)
            };
            _cancelButton.Click += OnCancelClick;

            footerPanel.Children.Add(_registerButton);
            footerPanel.Children.Add(_cancelButton);

            return footerPanel;
        }

        // Event Handlers with AI Validation
        private async void OnUsernameChanged(object sender, TextChangedEventArgs e)
        {
            await ValidateUsername();
        }

        private async void OnEmailChanged(object sender, TextChangedEventArgs e)
        {
            await ValidateEmail();
        }

        private async void OnPasswordChanged(object sender, TextChangedEventArgs e)
        {
            await ValidatePassword();
        }

        private async void OnConfirmPasswordChanged(object sender, TextChangedEventArgs e)
        {
            await ValidatePasswordConfirmation();
        }

        // AI-Powered Validation Methods
        private async Task ValidateUsername()
        {
            if (string.IsNullOrWhiteSpace(_usernameTextBox.Text))
            {
                _usernameState = ValidationState.Pending;
                return;
            }

            try
            {
                var context = new DecisionContext("Username_Validation", new { Username = _usernameTextBox.Text });
                var decision = await _aiCore.MakeDecisionAsync(context);

                _usernameState = decision.Confidence > 0.8 ? ValidationState.Valid :
                                decision.Confidence > 0.5 ? ValidationState.Warning :
                                ValidationState.Invalid;

                _logger.LogInformation("🤖 Username validation: {State} (Confidence: {Confidence})",
                    _usernameState, decision.Confidence);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Username validation error");
                _usernameState = ValidationState.Invalid;
            }

            UpdateFormValidation();
        }

        private async Task ValidateEmail()
        {
            if (string.IsNullOrWhiteSpace(_emailTextBox.Text))
            {
                _emailState = ValidationState.Pending;
                return;
            }

            try
            {
                // Basic email format validation
                var emailAttribute = new EmailAddressAttribute();
                bool isValidFormat = emailAttribute.IsValid(_emailTextBox.Text);

                var context = new DecisionContext("Email_Validation", new
                {
                    Email = _emailTextBox.Text,
                    IsValidFormat = isValidFormat
                });
                var decision = await _aiCore.MakeDecisionAsync(context);

                _emailState = decision.Confidence > 0.8 && isValidFormat ? ValidationState.Valid :
                             decision.Confidence > 0.5 ? ValidationState.Warning :
                             ValidationState.Invalid;

                _logger.LogInformation("📧 Email validation: {State} (Confidence: {Confidence})",
                    _emailState, decision.Confidence);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Email validation error");
                _emailState = ValidationState.Invalid;
            }

            UpdateFormValidation();
        }

        private async Task ValidatePassword()
        {
            if (string.IsNullOrWhiteSpace(_passwordTextBox.Text))
            {
                _passwordState = ValidationState.Pending;
                return;
            }

            try
            {
                var password = _passwordTextBox.Text;
                var strength = CalculatePasswordStrength(password);

                var context = new DecisionContext("Password_Validation", new
                {
                    Password = password,
                    Length = password.Length,
                    Strength = strength
                });
                var decision = await _aiCore.MakeDecisionAsync(context);

                _passwordState = strength >= 0.8 && decision.Confidence > 0.7 ? ValidationState.Valid :
                                strength >= 0.5 ? ValidationState.Warning :
                                ValidationState.Invalid;

                _logger.LogInformation("🔐 Password validation: {State} (Strength: {Strength})",
                    _passwordState, strength);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Password validation error");
                _passwordState = ValidationState.Invalid;
            }

            UpdateFormValidation();
        }

        private async Task ValidatePasswordConfirmation()
        {
            if (string.IsNullOrWhiteSpace(_confirmPasswordTextBox.Text))
            {
                _confirmPasswordState = ValidationState.Pending;
                return;
            }

            try
            {
                bool passwordsMatch = _passwordTextBox.Text == _confirmPasswordTextBox.Text;

                var context = new DecisionContext("Password_Confirmation", new
                {
                    PasswordsMatch = passwordsMatch,
                    ConfirmPassword = _confirmPasswordTextBox.Text
                });
                var decision = await _aiCore.MakeDecisionAsync(context);

                _confirmPasswordState = passwordsMatch && decision.Confidence > 0.9 ? ValidationState.Valid :
                                       ValidationState.Invalid;

                _logger.LogInformation("🔒 Password confirmation: {State} (Match: {Match})",
                    _confirmPasswordState, passwordsMatch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Password confirmation validation error");
                _confirmPasswordState = ValidationState.Invalid;
            }

            UpdateFormValidation();
        }

        private double CalculatePasswordStrength(string password)
        {
            double score = 0.0;

            if (password.Length >= 8) score += 0.25;
            if (password.Length >= 12) score += 0.15;
            if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]")) score += 0.20;
            if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[a-z]")) score += 0.20;
            if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[0-9]")) score += 0.15;
            if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[^A-Za-z0-9]")) score += 0.15;

            return Math.Min(score, 1.0);
        }

        private void UpdateFormValidation()
        {
            bool isFormValid = _usernameState == ValidationState.Valid &&
                              _emailState == ValidationState.Valid &&
                              _passwordState == ValidationState.Valid &&
                              _confirmPasswordState == ValidationState.Valid;

            _registerButton.IsEnabled = isFormValid;

            // Update validation summary UI
            Dispatcher.InvokeAsync(() =>
            {
                // Refresh validation summary display
                // This would update the visual indicators
            });
        }

        private async void OnRegisterClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _loadingIndicator.Visibility = Visibility.Visible;
                _loadingIndicator.StartAnimation();

                _logger.LogInformation("🚀 Neural registration process started");

                // AI-powered final validation
                var registrationContext = new DecisionContext("Registration_Final", new
                {
                    Username = _usernameTextBox.Text,
                    Email = _emailTextBox.Text,
                    FormValidation = new { _usernameState, _emailState, _passwordState, _confirmPasswordState }
                });

                var finalDecision = await _aiCore.MakeDecisionAsync(registrationContext);

                if (finalDecision.Confidence > 0.9)
                {
                    // Simulate registration process
                    await Task.Delay(2000);

                    MessageBox.Show(
                        "🎉 Registration successful!\n\n" +
                        $"AI Confidence: {finalDecision.Confidence:P0}\n" +
                        $"Recommendation: {finalDecision.Recommendation}",
                        "Neural Registration Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show(
                        $"❌ Registration validation failed.\n\n" +
                        $"AI Confidence: {finalDecision.Confidence:P0}\n" +
                        $"Reason: {finalDecision.Reasoning}",
                        "Neural Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Registration process error");
                MessageBox.Show("An error occurred during registration. Please try again.",
                    "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _loadingIndicator.StopAnimation();
                _loadingIndicator.Visibility = Visibility.Collapsed;
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public enum ValidationState
    {
        Pending,
        Valid,
        Invalid,
        Warning
    }
}
