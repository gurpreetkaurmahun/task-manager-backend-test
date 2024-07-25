using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Moq.EntityFrameworkCore;
using System.Linq;
using TaskManager.Models;
using TaskManager.Service;
using TaskManager.Helpers;
using Microsoft.EntityFrameworkCore;

public class SubTaskServiceTest
{
    private readonly Mock<TaskContext> _mockContext;
    private readonly Mock<ILogger<SubTaskService>> _mockLogger;
    private readonly SubTaskService _subTaskService;
    private readonly List<SubTask> _testSubTasks;
    private readonly List<TaskItem> _testTaskItems;

     public SubTaskServiceTest()
    {
        // Initialise test data for Subtasks
        _testSubTasks = new List<SubTask>
        {
            new SubTask
            {
                SubTaskId = 1,
                SubTaskName = "Test SubTask 1",
                SubTaskDescription = "Description for Test SubTask 1",
                DateCreated = new DateOnly(2024, 07, 24),
                DueDate = new DateOnly(2024, 07, 25),
                IsCompleted = true,
                TaskItemId = 1
            },
            new SubTask
            {
                SubTaskId = 2,
                SubTaskName = "Test SubTask 2",
                SubTaskDescription = "Description for Test SubTask 2",
                DateCreated = new DateOnly(2024, 07, 24),
                DueDate = new DateOnly(2024, 07, 25),
                IsCompleted = true,
                TaskItemId = 3
            }
        };

        // Initialise test data for TaskItems
        _testTaskItems = new List<TaskItem>
        {
            new TaskItem
            {
                TaskItemId = 1,
                TaskItemName = "Test Task Item 1",
                TaskItemDescription = "It is a unit test task",
                DateCreated = new DateOnly(2024, 07, 23),
                DueDate = new DateOnly(2024, 07, 28),
                IsCompleted = true,
                SubTasks = new List<SubTask>
                {
                    new SubTask
                    {
                        SubTaskId = 1,
                        SubTaskName = "SubTask 1",
                        SubTaskDescription = "SubDescription 1",
                        DateCreated = new DateOnly(2024, 07, 24),
                        DueDate = new DateOnly(2024, 07, 25),
                        IsCompleted = true,
                        TaskItemId = 1
                    }
                }
            },
            new TaskItem
            {
                TaskItemId = 2,
                TaskItemName = "Test Task Item 2",
                TaskItemDescription = "It is a unit test task no 2",
                DateCreated = new DateOnly(2024, 07, 26),
                DueDate = new DateOnly(2024, 07, 31),
                IsCompleted = false,
                SubTasks = new List<SubTask>()
            },
            new TaskItem
            {
                TaskItemId = 3,
                TaskItemName = "Test Task Item 3",
                TaskItemDescription = "It is a unit test task",
                DateCreated = new DateOnly(2024, 07, 23),
                DueDate = new DateOnly(2024, 07, 28),
                IsCompleted = true,
                SubTasks = new List<SubTask>
                {
                    new SubTask
                    {
                        SubTaskId = 2,
                        SubTaskName = "SubTask 2",
                        SubTaskDescription = "SubDescription 1",
                        DateCreated = new DateOnly(2024, 07, 24),
                        DueDate = new DateOnly(2024, 07, 25),
                        IsCompleted = true,
                        TaskItemId = 3
                    }
                }
            }
        };

        // Mock the DbContext
        _mockContext = new Mock<TaskContext>();
        _mockContext.Setup(c => c.SubTasks).ReturnsDbSet(_testSubTasks);
        _mockContext.Setup(c => c.TaskItems).ReturnsDbSet(_testTaskItems);

        // Mock the logger
        _mockLogger = new Mock<ILogger<SubTaskService>>();

        // Initialise the SubTaskService
        _subTaskService = new SubTaskService(_mockContext.Object, _mockLogger.Object);
    }


    [Fact]
    public void GetSubTaskWithId_SubTaskExists_ShouldReturnSubTask()
    {
        // Retrieve the SubTask with id 1
        var (subTask, message) = _subTaskService.GetSubTaskWithId(1);

        // Verify that the retrieved SubTask is not null and has the expected name
        Assert.NotNull(subTask);
        Assert.Equal("Test SubTask 1", subTask.SubTaskName);
        Assert.Contains("Successfully retrieved SubTask for Task with id: 1", message);
    }


    [Fact]
    public void GetSubTaskWithId_SubTaskDoesNotExist_ShouldReturnNull()
    {
        // Attempt to retrieve a SubTask with a non-existent id
        var (subTask, message) = _subTaskService.GetSubTaskWithId(3);

        // Verify that the retrieved SubTask is null and the message indicates subtask doesnot exist
        Assert.Null(subTask);
        Assert.Contains("SubTask with id: 3 doesnot exists", message);
    }


    [Fact]
    public void GetSubTasks_ExceptionThrown_ShouldReturnErrorMessage()
    {
        // Setup the context to throw an exception when accessing TaskItems
        _mockContext.Setup(c => c.TaskItems).Throws(new Exception("Database error"));

        // Attempt to retrieve SubTasks for Task with id 1
        var (subTasks, message) = _subTaskService.GetSubTasks(1);

        // Verify that the retrieved SubTasks are null and an error message is returned
        Assert.Null(subTasks);
        Assert.Contains("Failed to retreive SubTasks for Task with TaskId: 1", message);
    }


    [Fact]
    public void GetSubTasks_TaskDoesNotExist_ShouldReturnErrorMessage()
    {
        // Setup test data with only one TaskItem
        var taskItems = new List<TaskItem>
        {
            new TaskItem { TaskItemId = 1, TaskItemName = "Task 1" }
        };
        _mockContext.Setup(c => c.TaskItems).ReturnsDbSet(taskItems);

        // Attempt to retrieve SubTasks for a non-existent Task id
        var (subTasks, message) = _subTaskService.GetSubTasks(999);

        // Verify that the retrieved SubTasks are empty and an error message is returned
        Assert.Empty(subTasks);
        Assert.Contains("TaskItem with id: 999 doesnot exists, Please recheck the task id", message);
    }


    [Fact]
    public void UpdateSubTask_EmptySubTaskDescription_ShouldReturnErrorMessage()
    {
        // Create an updated SubTask with an empty description
        int taskId = 1;
        int subTaskId = 1;
        var updatedSubTask = new SubTask
        {
            SubTaskId = subTaskId,
            SubTaskName = "Updated SubTask",
            SubTaskDescription = "",
            DateCreated = new DateOnly(2024, 07, 24),
            DueDate = new DateOnly(2024, 07, 26),
            IsCompleted = false,
            TaskItemId = taskId
        };

        // Attempt to update the SubTask
        var (result, message) = _subTaskService.UpdateSubTask(taskId, subTaskId, updatedSubTask);

        // Verify that the update operation fails and an appropriate error message is returned
        Assert.False(result);
        Assert.Contains("The SubTask description cannot be left empty", message);
    }


    [Fact]
    public void AddSubTask_TaskDoesNotExist_ReturnsErrorMessage()
    {
        //Create a SubTask with a non-existent TaskItem ID
        var subTask = new SubTask { TaskItemId = 99, SubTaskName = "SubTask1", SubTaskDescription = "Description1" };

        //Attempt to add the SubTask
        var result = _subTaskService.AddSubTask(subTask);

        //Verify that the addiing the subtask fails and an error message is returned
        Assert.Null(result.subTask);
        Assert.Equal("  Cannot add SubTask,Task with id:99 doesnot exists, Please recheck the task Id", result.Message);
       
    }


    [Fact]
    public void AddSubTask_EmptySubTaskName_ReturnsErrorMessage()
    {

        //Create a SubTask with an empty name
        var taskItem = new TaskItem { TaskItemId = 1 };
        _testTaskItems.Add(taskItem);
        var subTask = new SubTask { TaskItemId = 1, SubTaskName = "", SubTaskDescription = "Description1" };

        //Attempt to add the SubTask
        var result = _subTaskService.AddSubTask(subTask);

        //Verify that the addiing the subtask fails and an error message is returned
        Assert.Null(result.subTask);
        Assert.Equal("The SubTask name cannot be left empty", result.Message);
       
    }
     
    [Fact]
    public void AddSubTask_ValidInput_AddsSubTaskSuccessfully()
    {
        //Create a new TaskItem and SubTask with valid inputs
        var taskItem = new TaskItem { TaskItemId = 1 };
        _testTaskItems.Add(taskItem);
        var subTask = new SubTask
        {
            TaskItemId = 1,
            SubTaskName = "SubTask1",
            SubTaskDescription = "Description1",
            DateCreated = new DateOnly(2024, 07, 24),
            DueDate = new DateOnly(2024, 07, 25)
        };

        // Act: Attempt to add the SubTask 
        var result = _subTaskService.AddSubTask(subTask);

        //Verify that the SubTask was added successfully and the correct message is returned
        Assert.NotNull(result.subTask);
        Assert.Equal(subTask.SubTaskName, result.subTask.SubTaskName);
        Assert.Equal($"TaskItem with name {subTask.SubTaskName} added successfully", result.Message);
       
    }


    [Fact]
    public void DeleteSubTask_TaskDoesNotExist_ReturnsErrorMessage()
    {
        // Define a non-existent TaskItem id and a valid SubTask id
        var nonExistentTaskId = 99;
        var subTaskId = 1;

        //Attempt to delete the SubTask associated with the non-existent TaskItem id
        var result = _subTaskService.DeleteSubTask(nonExistentTaskId, subTaskId);

        //Verify that the delete operation fails and an appropriate error message is returned
        Assert.False(result.result);
        Assert.Equal($"  Cannot add SubTask,Task with id: {nonExistentTaskId} doesnot exists, Please recheck the task Id", result.message);
        
    }
  
}
