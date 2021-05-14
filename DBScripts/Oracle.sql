declare
name_exists_exception exception; 
index_exists_exception exception; 
column_exists_exception exception; 
column_not_exists_exception exception; 
pragma exception_init( name_exists_exception, -955 );
pragma exception_init( index_exists_exception, -1408 );
pragma exception_init( column_exists_exception, -1430 );
pragma exception_init( column_not_exists_exception, -904 );
begin
    begin
        execute immediate 'CREATE TABLE Exceptions(
            Id number generated always as Identity(start with 1 increment by 1),
            GUID varchar2(36) NOT NULL,
            ApplicationName varchar2(50) NOT NULL,
            MachineName varchar2(50) NOT NULL,
            CreationDate timestamp NOT NULL,
            "TYPE" varchar2(100) NOT NULL,
            IsProtected number(1) default 0 NOT NULL,
            Host varchar2(100) NULL,
            Url varchar2(500) NULL,
            HTTPMethod varchar2(10) NULL,
            IPAddress varchar2(40) NULL,
            "SOURCE" varchar2(100) NULL,
            Message varchar2(1000) NULL,
            Detail Nclob NULL,	
            StatusCode number NULL,
            DeletionDate timestamp NULL,
            FullJson Nclob NULL,
            ErrorHash number NULL,
            DuplicateCount number default 1 NOT NULL,
            LastLogDate timestamp NULL,
            Category varchar2(100) NULL
        )';
        exception
            when name_exists_exception then
                null;
        end;

        begin
            execute immediate('CREATE INDEX IX_Exceptions_One ON Exceptions(GUID, ApplicationName, DeletionDate, CreationDate DESC)');
        exception
            when index_exists_exception then
                null;
            when name_exists_exception then
                null;
        end;

        begin
            execute immediate('CREATE INDEX IX_Exceptions_Two ON Exceptions(ErrorHash, ApplicationName, CreationDate DESC, DeletionDate)');
        exception
            when index_exists_exception then
                null;
            when name_exists_exception then
                null;
        end;

        --Begin V2 Schema changes

        begin
            execute immediate('ALTER TABLE Exceptions ADD LastLogDate timestamp NULL');
        exception
            when column_exists_exception then
                null;
        end;

        begin
            execute immediate('ALTER TABLE Exceptions ADD Category varchar2(100) NULL');
        exception
                when column_exists_exception then
                null;
        end;
                           
        begin
            execute immediate('ALTER TABLE Exceptions DROP COLUMN SQL');
        exception
            when column_not_exists_exception then
                null;
        end;
end;
/

create or replace
	function ExceptionsLogUpdate(p_DuplicateCount in number,
								 p_CreationDate in timestamp,
								 p_ErrorHash in number,
								 p_ApplicationName in varchar2, 
								 p_MinDate in timestamp)
	return varchar2 is
    pragma autonomous_transaction;
    begin
	    declare
		    v_guid varchar2(36);
	    begin
            
               update Exceptions
		       set DuplicateCount = DuplicateCount + p_DuplicateCount,
		       LastLogDate = (Case When LastLogDate Is Null Or p_CreationDate > LastLogDate Then p_CreationDate Else LastLogDate End)		     
		       where ErrorHash = p_ErrorHash
		       and ApplicationName = p_ApplicationName
		       and DeletionDate Is Null
		       and CreationDate >= p_MinDate
		       and rownum = 1
		       returning GUID into v_guid;
               commit;

		       return v_guid;
	    end;	
     end;