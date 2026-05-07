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

async function createAdmin() {
    try {
        await sql.connect(config)
        console.log("====== INSERTING ADMIN CREDENTIALS ======");

        try {
            await sql.query("INSERT INTO [Designation] (designation_name) VALUES ('System Administrator');");
        } catch (e) {
            // Might already exist
        }

        let desig = await sql.query("SELECT designation_id FROM [Designation] WHERE designation_name = 'System Administrator';");
        let dId = desig.recordset[0].designation_id;

        await sql.query(`INSERT INTO [Employee] (employee_name, email, username, epassword, designation_id) VALUES ('Admin User', 'admin@gpms.gov', 'admin', 'admin123', ${dId});`);

        console.log("SUCCESS! Admin user registered with username 'admin' and password 'admin123'");

    } catch (err) {
        if (err.message.includes("Violation of UNIQUE KEY constraint")) {
            console.log("User 'admin' already registered!");
        } else {
            console.error("SQL Error", err)
        }
    } finally {
        process.exit();
    }
}
createAdmin()
