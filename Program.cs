using CardBurner2.SmartCardHelperService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardBurner2
{
    class Program
    {
        /**
		 * Usage of the application
		 * -test to test the system
		 * -setup <<pin>> to get the Election Signature
		 * -status <<pin>> to get the status of the card inserted
		 * -check to check the type of card inserted
		 * <<pin>> <<VoterSessionId>> <<ProvisionalMode>> <<VerificationCode>> <<AvsMode>> <<ProvisionalCode(optional)>> to write the voter card
		 **/

        public static void Main(string[] args)
        {
            if (args == null || args.Length < 1)
            {
                Console.WriteLine("Error: Arguments must be supplied.");
                return;
            }
            Boolean successful = false;
            try
            {
                using (SmartCardHelperClient helper = new SmartCardHelperClient())
                {
                    // Setup the object and check that a card is present
                    if (helper == null)
                    {
                        Console.WriteLine("Error: No SmartCardHelperClient");
                        return;
                    }
                    helper.SelectCardReader(string.Empty);

                    String[] readers = helper.CardReadersList();
                    if (readers.Length == 0)
                    {
                        // No Readers throw an error
                        Console.WriteLine("Error: No Card Reader Found");
                        helper.SelectCardReader(string.Empty);
                        return;
                    }
                    else
                    {
                        // Set card reader to the first value
                        helper.SelectCardReader(readers[0]);
                    }

                    if (!helper.CardPresent())
                    {
                        // No Card throw an error
                        Console.WriteLine("Error: No Card Present");
                        helper.SelectCardReader(string.Empty);
                        return;
                    }

                    // Execute the request of the call
                    if (args.Length == 1 && args[0].Equals("-test"))
                    {
                        Console.WriteLine("Success");
                        helper.SelectCardReader(string.Empty);
                        return;
                    }
                    else if (args.Length == 1 && args[0].Equals("-check"))
                    {
                        checkCardType(helper);
                        helper.SelectCardReader(string.Empty);
                        return;
                    }
                    else if (args.Length == 2 && args[0].Equals("-setup"))
                    {
                        readPollworkerCard(helper, args[1]);
                        helper.SelectCardReader(string.Empty);
                        return;
                    }
                    else if (args.Length == 2 && args[0].Equals("-status"))
                    {
                        Console.WriteLine(getCardStatus(helper, args[1]));
                        helper.SelectCardReader(string.Empty);
                        return;
                    }
                    else if (args.Length < 5)
                    {
                        // What should be in this message?
                        Console.WriteLine("Error: Five arguments must be provided, the sixth is optional");
                        helper.SelectCardReader(string.Empty);
                        return;
                    }

                    // Write the card for a check-in
                    string pin = args[0];
                    string voterSessionId = args[1];
                    int provisionalMode = int.Parse(args[2]);
                    string verificationCode = args[3];
                    string avsMode = args[4];
                    string provisionalCode = string.Empty;
                    const string dateFormat = "yyyyMMddHHmmss";
                    if (provisionalMode > 0)
                    {
                        if (args.Length < 6)
                        {
                            Console.WriteLine("Error: Provisional Code is required when Provisional Mode is 1 or 2");
                            return;
                        }
                        provisionalCode = args[5];
                    }

                    if (helper.CheckCardType().Equals(EInitSCardType.AdminCard))
                    {
                        // Pollworker Card, don't overwrite this type of card
                        Console.WriteLine("Error: Pollworker Card Present");
                        helper.SelectCardReader(string.Empty);
                        return;
                    }

                    // Build the VoterFile object
                    VoterFile voterFile = new VoterFile();
                    voterFile.VoterSessionId = voterSessionId;
                    voterFile.ProvisionalMode = provisionalMode;
                    if (provisionalMode > 0)
                        voterFile.ProvisionalCode = provisionalCode;
                    else
                        voterFile.ProvisionalCode = string.Empty;
                    try
                    {
                        voterFile.VerificationCode = Convert.FromBase64String(verificationCode);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: Failed to decode Election Signature: " + e.StackTrace);
                        helper.SelectCardReader(string.Empty);
                        return;
                    }
                    voterFile.AvsMode = avsMode;
                    string creationTimeStamp = DateTime.Now.ToString(dateFormat);
                    voterFile.CreationTimeStamp = creationTimeStamp;
                    voterFile.ErrorCode = string.Empty;
                    voterFile.VotingSessionStatus = 0;

                    if (voterFile != null)
                    {
                        EResultCode result = helper.WriteVoterFile(pin, voterFile, true);
                        if (result != EResultCode.Ok)
                        {
                            Console.WriteLine("Error: Failed to write voter card");
                            helper.SelectCardReader(string.Empty);
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error: Getting Voter File.");
                        helper.SelectCardReader(string.Empty);
                        return;
                    }
                    Console.WriteLine("Success");
                    successful = true;
                    helper.SelectCardReader(string.Empty);
                }
                if (!successful)
                {
                    Console.WriteLine("Error: Getting Helper Service Object");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: Exception when testing system!");
                Console.WriteLine("Exception text: " + e.Message);
            }
        }

        // Convert Hex String to Byte Array
        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
            {
                try
                {
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            return bytes;
        }

        protected static void checkCardType(SmartCardHelperClient helper)
        {
            try
            {
                // What type of Card is inserted
                if (helper.CheckCardType().Equals(EInitSCardType.AdminCard))
                {
                    // It is a Pollworker Card
                    Console.WriteLine("Pollworker Card");
                    return;
                }
                else if (helper.CheckCardType().Equals(EInitSCardType.VoterCard))
                {
                    // It is a Voter Card
                    Console.WriteLine("Voter Card");
                    return;
                }
                else if (helper.CheckCardType().Equals(EInitSCardType.EmptyCard))
                {
                    // It is a Empty Card
                    Console.WriteLine("Empty Card");
                    return;
                }
                else if (helper.CheckCardType().Equals(EInitSCardType.UnknownCard))
                {
                    // It is an unknown card
                    Console.WriteLine("Unknown Card");
                    return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: Checking Card Type - " + e.StackTrace);
                return;
            }
        }

        protected static string getCardStatus(SmartCardHelperClient helper, string pin)
        {
            string response = "";
            try
            {
                // What type of Card is inserted
                if (helper.CheckCardType().Equals(EInitSCardType.AdminCard))
                {
                    // It is a Pollworker Card
                    response = getPollworkerCardDetails(helper, pin);
                }
                else if (helper.CheckCardType().Equals(EInitSCardType.VoterCard))
                {
                    // It is a Voter Card
                    response = getVoterCardDetails(helper, pin);
                }
                else if (helper.CheckCardType().Equals(EInitSCardType.EmptyCard))
                {
                    // It is a Empty Card
                    response = "<cardType>empty</cardType>";
                }
                else if (helper.CheckCardType().Equals(EInitSCardType.UnknownCard))
                {
                    // It is an unknown card
                    response = "<cardType>unknown</cardType>";
                }
            }
            catch (Exception e)
            {
                response = "Error: Checking Card Type - " + e.StackTrace;
            }

            return response;
        }

        protected static string getPollworkerCardDetails(SmartCardHelperClient helper, string pin)
        {
            int retries = 3;
            string details;
            try
            {
                AdminFile pwCard = helper.ReadAdminFile(pin, out retries);
                details = "<cardType>pollworker</cardType>";
                if (pwCard != null && (pwCard.ErrorCode == null || pwCard.ErrorCode.Equals(string.Empty)))
                {
                    // Write out the Election Signature
                    details += "<signature>";
                    details += BitConverter.ToString(pwCard.ElectionSignature).Replace("-", string.Empty);
                    details += "</signature>";
                }
                else if (pwCard != null)
                {
                    details = "Error: Reading Pollworker Card - " + pwCard.ErrorCode;
                }
                else
                {
                    details = "Error: Reading Pollworker Card";
                }
            }
            catch (Exception e)
            {
                details = "Error: Reading Pollworker Card - " + e.StackTrace;
            }

            return details;
        }

        protected static string getVoterCardDetails(SmartCardHelperClient helper, string pin)
        {
            int retries = 3;
            string details;
            try
            {
                VoterFile vCard = helper.ReadVoterFile(pin,out retries);
                ;
                details = "<cardType>voter</cardType>";
                if (vCard != null)
                {
                    // Voter Session Id
                    details += "<voterSessionId>" + vCard.VoterSessionId + "</voterSessionId>";
                    // Provisional Mode
                    details += "<provisionalMode>" + Convert.ToString(vCard.ProvisionalMode) + "</provisionalMode>";
                    // Provisional Code
                    details += "<provisionalCode>" + vCard.ProvisionalCode + "</provisionalCode>";
                    // Avs Mode
                    string avsMode = "";
                    if (vCard.AvsMode != null)
                        avsMode = vCard.AvsMode;
                    details += "<avsMode>" + avsMode + "</avsMode>";
                    // Language
                    string langStr = "";
                    if (vCard.Language != null)
                        langStr = vCard.Language;
                    details += "<language>" + langStr + "</language>";
                    // Voting Session Status
                    string votingSessionStatus = "";
                    if (vCard.VotingSessionStatus != null)
                        votingSessionStatus = Convert.ToString(vCard.VotingSessionStatus);
                    details += "<votingSessionStatus>" + votingSessionStatus + "</votingSessionStatus>";
                    // Error Code
                    string errorCode = "";
                    if (vCard.ErrorCode != null)
                        errorCode = vCard.ErrorCode;
                    details += "<errorCode>" + errorCode + "</errorCode>";
                    // Creation Time Stamp
                    string creationTimeStamp = "";
                    if (vCard.CreationTimeStamp != null)
                        creationTimeStamp = vCard.CreationTimeStamp;
                    details += "<creationTimeStamp>" + creationTimeStamp + "</creationTimeStamp>";
                    string votedTimeStamp = "";
                    if (vCard.VotingEndTime != null)
                        votedTimeStamp = vCard.VotingEndTime;
                    details += "<votedTimeStamp>" + votedTimeStamp + "</votedTimeStamp>";
                    string votedMachine = "";
                    if (vCard.TabulatorNumber != null)
                        votedMachine = Convert.ToString(vCard.TabulatorNumber);
                    details += "<votedMachine>" + votedMachine + "</votedMachine>";
                    // Verification Code
                    details += "<signature>";
                    details += BitConverter.ToString(vCard.VerificationCode).Replace("-", string.Empty);
                    details += "</signature>";
                }
                else
                {
                    details = "Error: Reading Voter Card";
                }
            }
            catch (Exception e)
            {
                details = "Error: Reading Voter Card - " + e.StackTrace;
            }

            return details;
        }

        protected static void readPollworkerCard(SmartCardHelperClient helper, string pin)
        {
            int retries = 3;
            try
            {
                // Is it a pollworker card?
                if (!helper.CheckCardType().Equals(EInitSCardType.AdminCard))
                {
                    // Not a Pollworker Card
                    Console.WriteLine("Error: Not a Pollworker Card");
                    return;
                }

                AdminFile pwCard = helper.ReadAdminFile(pin, out retries);
                if (pwCard != null && (pwCard.ErrorCode == null || pwCard.ErrorCode.Equals(string.Empty)))
                {
                    // Write out the Election Signature
                    Console.WriteLine(Convert.ToBase64String(pwCard.ElectionSignature));
                    return;
                }
                else if (pwCard != null)
                {
                    Console.WriteLine("Error: Reading Pollworker Card - " + pwCard.ErrorCode);
                    return;
                }
                else
                {
                    Console.WriteLine("Error: Reading Pollworker Card");
                    return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: Reading Pollworker Card - " + e.StackTrace);
                return;
            }
        }
    }
}