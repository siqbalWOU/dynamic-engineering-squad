using InfrastructureApp.Controllers;
using InfrastructureApp.Services;
using InfrastructureApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Reqnroll;

namespace InfrastructureApp_Tests.StepDefinitions
{
    [Binding]
    public class RoadCameraSteps
    {
        private Mock<ITripCheckService> _mockService = new();
        private RoadCameraController _controller = null!;
        private IActionResult _result = null!;

        public RoadCameraSteps()
        {
            _controller = new RoadCameraController(_mockService.Object);
        }

        // ── Givens ──

        [Given("the TripCheck API returns a list of cameras")]
        public void GivenApiReturnsCameras()
        {
            var cameras = new List<RoadCameraViewModel>
            {
                new() { CameraId = "CAM001", Name = "Camera 1", Road = "I-5",
                        ImageUrl = "http://example.com/cam1.jpg",
                        LastUpdated = DateTimeOffset.UtcNow },
                new() { CameraId = "CAM002", Name = "Camera 2", Road = "I-84",
                        ImageUrl = "http://example.com/cam2.jpg",
                        LastUpdated = DateTimeOffset.UtcNow }
            };

            _mockService
                .Setup(s => s.GetCamerasAsync())
                .ReturnsAsync(cameras);
        }

        [Given("the TripCheck API returns no cameras")]
        public void GivenApiReturnsNoCameras()
        {
            _mockService
                .Setup(s => s.GetCamerasAsync())
                .ReturnsAsync(new List<RoadCameraViewModel>());
        }

        [Given("the TripCheck API returns a camera with id {string}")]
        public void GivenApiReturnsCameraWithId(string id)
        {
            var camera = new RoadCameraViewModel
            {
                CameraId    = id,
                Name        = "Test Camera",
                Road        = "I-5",
                ImageUrl    = "http://example.com/cam.jpg",
                LastUpdated = DateTimeOffset.UtcNow
            };

            _mockService
                .Setup(s => s.GetCameraByIdAsync(id))
                .ReturnsAsync(camera);
        }

        [Given("the TripCheck API returns no camera with id {string}")]
        public void GivenApiReturnsNoCameraWithId(string id)
        {
            _mockService
                .Setup(s => s.GetCameraByIdAsync(id))
                .ReturnsAsync((RoadCameraViewModel?)null);
        }

        // ── Whens ──

        [When("a user visits the Road Camera index page")]
        public async Task WhenUserVisitsIndex()
        {
            _result = await _controller.Index();
        }

        [When("a user views the details for camera {string}")]
        public async Task WhenUserViewsDetails(string id)
        {
            _result = await _controller.Details(id);
        }

        [When("the image is refreshed for camera {string}")]
        public async Task WhenImageIsRefreshed(string id)
        {
            _result = await _controller.RefreshImage(id);
        }

        // ── Thens ──

        [Then("the view model should contain cameras")]
        public void ThenViewModelShouldContainCameras()
        {
            var vm = GetIndexViewModel();
            Assert.That(vm.Cameras, Is.Not.Empty);
        }

        [Then("the view model should contain no cameras")]
        public void ThenViewModelShouldContainNoCameras()
        {
            var vm = GetIndexViewModel();
            Assert.That(vm.Cameras, Is.Empty);
        }

        [Then("the details view model should contain the camera")]
        public void ThenDetailsViewModelShouldContainCamera()
        {
            var vm = GetDetailsViewModel();
            Assert.That(vm.Camera, Is.Not.Null);
        }

        [Then("the details view model camera should be null")]
        public void ThenDetailsViewModelCameraShouldBeNull()
        {
            var vm = GetDetailsViewModel();
            Assert.That(vm.Camera, Is.Null);
        }

        [Then("no error message should be shown")]
        public void ThenNoErrorMessageShown()
        {
            if (_result is ViewResult vr)
            {
                var model = vr.Model;
                if (model is RoadCameraIndexViewModel index)
                    Assert.That(index.ErrorMessage, Is.Null);
                else if (model is RoadCameraDetailsViewModel details)
                    Assert.That(details.ErrorMessage, Is.Null);
            }
        }

        [Then("the camera error message should be {string}")]
        public void ThenErrorMessageShouldBe(string expected)
        {
            if (_result is ViewResult vr)
            {
                var model = vr.Model;
                if (model is RoadCameraIndexViewModel index)
                    Assert.That(index.ErrorMessage, Is.EqualTo(expected));
                else if (model is RoadCameraDetailsViewModel details)
                    Assert.That(details.ErrorMessage, Is.EqualTo(expected));
            }
        }

        [Then("the refresh response should contain a cameraId")]
        public void ThenRefreshResponseContainsCameraId()
        {
            var json = GetRefreshJson();
            Assert.That(json.ContainsKey("cameraId"), Is.True);
        }

        [Then("the refresh response should contain an imageUrl")]
        public void ThenRefreshResponseContainsImageUrl()
        {
            var json = GetRefreshJson();
            Assert.That(json.ContainsKey("imageUrl"), Is.True);
        }

        [Then("the refresh response should contain an error")]
        public void ThenRefreshResponseContainsError()
        {
            var json = GetRefreshJson();
            Assert.That(json.ContainsKey("error"), Is.True);
        }

        // ── Helpers ──

        private RoadCameraIndexViewModel GetIndexViewModel()
        {
            var view = _result as ViewResult;
            Assert.That(view, Is.Not.Null, "Result was not a ViewResult");
            var vm = view!.Model as RoadCameraIndexViewModel;
            Assert.That(vm, Is.Not.Null, "Model was not RoadCameraIndexViewModel");
            return vm!;
        }

        private RoadCameraDetailsViewModel GetDetailsViewModel()
        {
            var view = _result as ViewResult;
            Assert.That(view, Is.Not.Null, "Result was not a ViewResult");
            var vm = view!.Model as RoadCameraDetailsViewModel;
            Assert.That(vm, Is.Not.Null, "Model was not RoadCameraDetailsViewModel");
            return vm!;
        }

        private Dictionary<string, object> GetRefreshJson()
        {
            var json = _result as JsonResult;
            Assert.That(json, Is.Not.Null, "Result was not a JsonResult");
            var dict = json!.Value as Dictionary<string, object>;
            Assert.That(dict, Is.Not.Null, "JSON value was not a Dictionary");
            return dict!;
        }
    }
}