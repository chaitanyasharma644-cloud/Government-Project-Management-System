const sql = require('mssql');

const config = {
    user: 'sa',
    password: 'SuperStrong!Passw0rd',
    server: 'localhost',
    database: 'GPMS_Desktop',
    options: {
        encrypt: false,
        trustServerCertificate: true
    }
};

async function injectData() {
    try {
        await sql.connect(config)
        
        // 1. Get or Insert Designation
        let desig = await sql.query("SELECT designation_id FROM [Designation] WHERE designation_name = 'System Administrator';");
        if (desig.recordset.length === 0) {
            await sql.query("INSERT INTO [Designation] (designation_name, designation_description) VALUES ('System Administrator', 'System Administrator');");
            desig = await sql.query("SELECT designation_id FROM [Designation] WHERE designation_name = 'System Administrator';");
        }
        let dId = desig.recordset[0].designation_id;
        
        // 2. Insert User
        try {
            await sql.query(`INSERT INTO [Employee] (employee_name, email, username, epassword, designation_id) VALUES ('Chaitanya Sharma', 'chaitanya@gpms.gov', 'chaitanya', 'Pass@123', ${dId});`);
            console.log("Success: Injected User 'chaitanya'");
        } catch(e) {
            console.log("User 'chaitanya' likely already exists.");
        }
        
        // 3. Insert Projects
        try {
            await sql.query(`INSERT INTO [Project] (project_name, project_details, project_status, project_start_date) VALUES ('Smart City Infrastructure', 'Development of interconnected traffic and utility grids', 'Active', '2026-01-15');`);
            await sql.query(`INSERT INTO [Project] (project_name, project_details, project_status, project_start_date) VALUES ('National Health Tracking', 'Centralized DB for vaccination and health records', 'Planning', '2026-06-01');`);
            await sql.query(`INSERT INTO [Project] (project_name, project_details, project_status, project_start_date) VALUES ('Rural Broadband Initiative', 'Expansion of fiber optic cables to rural sectors', 'In Progress', '2025-11-20');`);
            console.log("Success: Injected 3 Mock Projects!");
        } catch(e) {
            console.log("Projects likely already exist.");
        }
        
    } catch (err) {
        console.error("SQL Error", err)
    } finally {
        process.exit();
    }
}
injectData()
