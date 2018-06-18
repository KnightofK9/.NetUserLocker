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
            String fileName = file.FullName + "_" + guid.ToString() + ".aes";
            String outputPath = Path.Combine(file.DirectoryName, fileName);
            encryptService.FileEncrypt(filePath, outputPath, Constant.DECRYPT);
        }
        public async Task<Boolean> DecryptFile(String imagePath, String filePath)
        {
            FileInfo file = new FileInfo(filePath);
            string guidString = file.Name.Split('_').Last().Split('.').First();
            Guid guid = Guid.Parse(guidString);
            if (await verificationFace(imagePath, guid, Constant.GROUP_ID))
            {
                String outputPath = Path.Combine(file.DirectoryName , file.Name.Split('_')[0]);
                encryptService.FileDecrypt(filePath, outputPath, Constant.DECRYPT);
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
            if (result != null)
            {
                personalItem.guid = result.PersonId;
                Console.WriteLine("Create person completed!");
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

        public async Task<Guid> detectPerson(String verifyImagePath, String groupId)
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
                        return identifyResult.Candidates[0].PersonId;
                    }
                }
            }
            return Guid.Empty;
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
            await createGroupAsync(groupId);
//            var result = await faceServiceClient.GetPersonGroupAsync(groupId);
//            if (result == null)
//            {
//                Console.WriteLine("No group, creating group now!");
//                await createGroupAsync(groupId);
//            }
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
            await faceServiceClient.TrainPersonGroupAsync(groupId);
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


        public async Task ResetTrainingAsync()
        {
            await this.faceServiceClient.DeletePersonGroupAsync(Constant.GROUP_ID);
        }
    }
}
