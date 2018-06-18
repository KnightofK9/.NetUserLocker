using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileLockWPF.service;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;

namespace FileLockWPF
{
    class FaceLockService
    {
        private readonly IFaceServiceClient faceServiceClient;
        private readonly EncryptService encryptService;

        public FaceLockService()
        {
            faceServiceClient = new FaceServiceClient(Constant.KEY, "https://southeastasia.api.cognitive.microsoft.com/face/v1.0");
            encryptService = new EncryptService();
        }

        public async Task EncryptFile(String filePath, Guid guid)
        {
            FileInfo file = new FileInfo(filePath);
            String outputPath = file.DirectoryName + "/" + file.FullName + "_" + guid.ToString() + ".aes";
            encryptService.FileEncrypt(filePath, outputPath, Constant.DECRYPT);
        }
        public async Task<Boolean> DecryptFile(String imagePath, String filePath)
        {
            FileInfo file = new FileInfo(filePath);
            Guid guid = Guid.Parse(file.Name.Split('_').Last());
            if (verificationFace(imagePath, guid, Constant.GROUP_ID).Result)
            {
                String outputPath = file.DirectoryName + "/" + file.Name.Split('_')[0];
                encryptService.FileDecrypt(filePath, outputPath, Constant.KEY);
                return true;
            }
            return false;
        }

        public async Task<CreatePersonResult> intPersonalItemOnServiceAsync(String personName, List<String> imagePaths)
        {
            PersonalItem personalItem = PersonalItem.CreateNewPersonalItem(personName, imagePaths);
            await createGroupIfNotExists(personalItem.personGroupId);
            Console.WriteLine("Init group completed!");
            var result = await createPersonAsync(personalItem);
            Console.WriteLine("Create person completed!");
            if (result != null)
            {
                await addPersonImageToGroup(personalItem);    
                Console.WriteLine("Upload person image completed!");
                await trainNetwork(Constant.GROUP_ID);
                Console.WriteLine("Train network completed!");
            }
            else
            {
                return null;
            }
            return result;

        }

        public async Task<Boolean> verificationFace(String verifyImagePath, Guid personGuid, String groupId)
        {

            using (Stream s = File.OpenRead(verifyImagePath))
            {
                var faces = await faceServiceClient.DetectAsync(s);
                var faceIds = faces.Select(face => face.FaceId).ToArray();

                var results = await faceServiceClient.IdentifyAsync(faceIds, groupId);
                foreach (var identifyResult in results)
                {
                    Console.WriteLine("Result of face: {0}", identifyResult.FaceId);
                    if (identifyResult.Candidates.Length == 0)
                    {
                        Console.WriteLine("No one identified");
                    }
                    else
                    {
                        // Get top 1 among all candidates returned
                        foreach (var candidate in identifyResult.Candidates)
                        {
                            var candidateId = candidate.PersonId;
                            if (candidateId == personGuid)
                            {
                                Console.WriteLine("Person found");
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private async Task createGroupIfNotExists(String groupId)
        {
            var result = await faceServiceClient.GetPersonGroupAsync(groupId);
            if (result == null)
            {
                Console.WriteLine("No group, creating group now!");
                await createGroupAsync(groupId);
            }
        }

        private async Task createGroupAsync(String groupId)
        {
            await faceServiceClient.CreatePersonGroupAsync(groupId, "User lock group");
        }
        
        private async Task<CreatePersonResult> createPersonAsync(PersonalItem personalItem)
        {
            CreatePersonResult personResult = await faceServiceClient.CreatePersonInPersonGroupAsync(
                personalItem.personGroupId,
                personalItem.name
            );
            return personResult;
        }

        private async Task addPersonImageToGroup(PersonalItem personalItem)
        {
            foreach (string imagePath in personalItem.imagePaths)
            {
                using (Stream s = File.OpenRead(imagePath))
                {
                    // Detect faces in the image and add to Anna
                    await faceServiceClient.AddPersonFaceInPersonGroupAsync(
                        personalItem.personGroupId, personalItem.guid, s);
                }
            }
        }

        private async Task trainNetwork(String groupId)
        {
            TrainingStatus trainingStatus = null;
            while (true)
            {
                trainingStatus = await faceServiceClient.GetPersonGroupTrainingStatusAsync(groupId);

                if (trainingStatus.Status != Status.Running)
                {
                    break;
                }

                await Task.Delay(1000);
            }
        }

   

    }
}
