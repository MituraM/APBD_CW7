SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION
	DECLARE @NextSemester INT = @CurrentSemester + 1;

	-- Get the next enrollment
	DECLARE @NextEnrollmentId INT;
	SELECT @NextEnrollmentId = IdEnrollment FROM Enrollment e
	WHERE e.Semester = @NextSemester AND e.IdStudy = @IdStudy

	-- Insert new enrollment
	IF @NextEnrollmentId IS NULL
		BEGIN
			-- Check the latest id number
			SELECT @NextEnrollmentId = (MAX(e.IdEnrollment) + 1) FROM Enrollment e;

			INSERT INTO Enrollment (IdEnrollment, Semester, IdStudy, StartDate)
			VALUES (@NextEnrollmentId, @NextSemester, @IdStudy, GETDATE());
		END

	-- Update students
	UPDATE Student
	SET IdEnrollment = @NextEnrollmentId
	WHERE IdEnrollment = (
		SELECT TOP 1 IdEnrollment FROM Enrollment e
		WHERE e.Semester = @CurrentSemester AND e.IdStudy = @IdStudy
		ORDER BY e.StartDate DESC
	);
COMMIT TRANSACTION
