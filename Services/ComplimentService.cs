using System;

namespace Tuck.Services
{
    public class ComplimentService
    {
        public static string GetCompliment() {

            string[] compliment = new string[10];
            compliment[0] = "Your hair smells nice";
            compliment[1] = "Your face is ok I guess";
            compliment[2] = "You're probably smarter than a penguin";
            compliment[3] = "Your body appears to be human";
            compliment[4] = "You seem pleasant";
            compliment[5] = "You likely aren't the most annoying person in the world";
            compliment[6] = "You are talanted... at something possibly";
            compliment[7] = "Your eyes are of good color";
            compliment[8] = "Your are the best looking person in a room by yourself";
            compliment[9] = "You are right all the time, exept only when you are not";

            int rng = new Random().Next(0, 10);
            string _result = compliment[rng];

            return _result;
        }
    }
}