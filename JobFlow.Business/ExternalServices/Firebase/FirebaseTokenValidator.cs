using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.ExternalServices.Firebase
{
    public interface IFirebaseTokenValidator
    {
        Task<GoogleJsonWebSignature.Payload> ValidateTokenAsync(string idToken);
    }

    public class FirebaseTokenValidator : IFirebaseTokenValidator
    {
        private readonly string _firebaseProjectId;

        public FirebaseTokenValidator(IConfiguration config)
        {
            _firebaseProjectId = config["Firebase:ProjectId"];
        }

        public async Task<GoogleJsonWebSignature.Payload> ValidateTokenAsync(string idToken)
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new[] { _firebaseProjectId }
            };

            return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }
    }

}
