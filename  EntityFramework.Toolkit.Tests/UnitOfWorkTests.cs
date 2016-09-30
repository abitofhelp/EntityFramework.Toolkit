﻿using System;
using System.Collections.Generic;
using EntityFramework.Toolkit.Core;
using EntityFramework.Toolkit.Exceptions;

using FluentAssertions;

using Moq;

using ToolkitSample.DataAccess.Context;

using Xunit;

namespace EntityFramework.Toolkit.Tests
{
    public class UnitOfWorkTests
    {
        [Fact]
        public void ShouldCommitToSingleContext()
        {
            // Arrange
            var unitOfWork = new UnitOfWork();
            var sampleContextMock = new Mock<ISampleContext>();

            unitOfWork.RegisterContext(sampleContextMock.Object);

            // Act
            unitOfWork.Commit();

            // Assert
            sampleContextMock.Verify(x => x.SaveChanges(), Times.Once);
        }

        [Fact]
        public void ShouldCommitToMultipleContexts()
        {
            // Arrange
            var unitOfWork = new UnitOfWork();
            var sampleContextOneMock = new Mock<ISampleContext>();
            var sampleContextTwoMock = new Mock<ISampleContextTwo>();

            unitOfWork.RegisterContext(sampleContextOneMock.Object);
            unitOfWork.RegisterContext(sampleContextTwoMock.Object);

            // Act
            unitOfWork.Commit();

            // Assert
            sampleContextOneMock.Verify(x => x.SaveChanges(), Times.Once);
            sampleContextTwoMock.Verify(x => x.SaveChanges(), Times.Once);
        }

        [Fact]
        public void ShouldFailToCommitMultipleContexts()
        {
            // Arrange
            var unitOfWork = new UnitOfWork();
            var sampleContextOneMock = new Mock<ISampleContext>();
            var sampleContextTwoMock = new Mock<ISampleContextTwo>();
            sampleContextTwoMock.Setup(m => m.SaveChanges()).Throws(new InvalidOperationException("SampleContextOne failed to SaveChanges."));

            unitOfWork.RegisterContext(sampleContextOneMock.Object);
            unitOfWork.RegisterContext(sampleContextTwoMock.Object);

            // Act
            Action action = () => unitOfWork.Commit();

            // Assert
            action.ShouldThrow<UnitOfWorkException>().WithInnerException<InvalidOperationException>();
            sampleContextOneMock.Verify(x => x.SaveChanges(), Times.Once);
            sampleContextTwoMock.Verify(x => x.SaveChanges(), Times.Once);
        }

        [Fact]
        public void ShouldCommitNoChangesWhenNothingNeedsToBeDone()
        {
            // Arrange
            using (IUnitOfWork unitOfWork = new UnitOfWork())
            {
                // Act
                var numberOfChanges = unitOfWork.Commit();

                // Assert
                numberOfChanges.Should().HaveCount(0);
            }
        }

        [Fact]
        public void ShouldCommitChangesOfContext()
        {
            // Arrange
            IUnitOfWork unitOfWork = new UnitOfWork();

            var contextMock = new Mock<IContext>();
            var changeSet = new ChangeSet(contextMock.GetType(), new List<IChange> { Change.CreateAddedChange(new object()) });
            contextMock.Setup(c => c.SaveChanges()).Returns(changeSet);

            unitOfWork.RegisterContext(contextMock.Object);

            // Act
            var numberOfChanges = unitOfWork.Commit();

            // Assert
            numberOfChanges.Should().HaveCount(1);
        }

        [Fact]
        public void ShouldHandleMultipleInstances()
        {
            // Arrange
            using (IUnitOfWork outerUnitOfWork = new UnitOfWork())
            {
                using (IUnitOfWork innerUnitofWork = new UnitOfWork())
                {
                    // Act
                    var changeSets = innerUnitofWork.Commit();

                    // Assert
                    changeSets.Should().HaveCount(0);
                }
                outerUnitOfWork.Commit();
            }
        }

        //TODO Write test to save + check summary of changes
        //TODO Write test to saveasync + check summary of changes
    }
}
