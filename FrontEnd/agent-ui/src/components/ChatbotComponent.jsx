import { useState, useEffect } from 'react';
import axios from 'axios';

const API_BASE_URL = 'https://localhost:5001/api';

const ChatbotComponent = () => {
  const [query, setQuery] = useState('');
  const [responses, setResponses] = useState([]);
  const [loading, setLoading] = useState(false);
  const [companies, setCompanies] = useState([]);
  const [selectedCompany, setSelectedCompany] = useState(null);
  const [token, setToken] = useState(localStorage.getItem('token'));
  const [isLoggedIn, setIsLoggedIn] = useState(!!localStorage.getItem('token'));
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [loginLoading, setLoginLoading] = useState(false);
  const [loginError, setLoginError] = useState('');

  // Load companies when token changes
  useEffect(() => {
    if (!token) {
      return;
    }

    const loadCompanies = async () => {
      try {
        const response = await axios.get(`${API_BASE_URL}/chatbot/companies`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        if (response.data && response.data.data) {
          setCompanies(response.data.data);
        }
      } catch (error) {
        console.error('Error loading companies:', error);
      }
    };

    loadCompanies();
  }, [token]);

  const handleLogin = async (e) => {
    e.preventDefault();
    setLoginLoading(true);
    setLoginError('');

    try {
      const response = await axios.post(`${API_BASE_URL}/auth/login`, {
        username,
        password
      });

      if (response.data.success) {
        const newToken = response.data.token;
        localStorage.setItem('token', newToken);
        setToken(newToken);
        setIsLoggedIn(true);
        setUsername('');
        setPassword('');
      } else {
        setLoginError(response.data.message || 'Login failed');
      }
    } catch (error) {
      console.error('Login error:', error);
      setLoginError(error.response?.data?.message || 'Invalid credentials');
    } finally {
      setLoginLoading(false);
    }
  };

  const handleLogout = () => {
    localStorage.removeItem('token');
    setToken(null);
    setIsLoggedIn(false);
    setResponses([]);
    setCompanies([]);
    setUsername('');
    setPassword('');
  };

  const handleSendQuery = async (e) => {
    e.preventDefault();
    
    if (!query.trim()) {
      console.log('Query is empty');
      return;
    }

    if (!token) {
      console.log('No token available');
      return;
    }

    setLoading(true);
    
    try {
      const queryText = query;
      const companyId = selectedCompany;
      
      console.log('Sending query:', { queryText, companyId, token: token.substring(0, 20) + '...' });
      
      const response = await axios.post(
        `${API_BASE_URL}/chatbot/query`,
        {
          query: queryText,
          companyId: companyId || null
        },
        {
          headers: { Authorization: `Bearer ${token}` }
        }
      );

      console.log('Query response:', response.data);

      if (response.data?.data) {
        const chatResponse = response.data.data;
        const newResponse = {
          userQuery: chatResponse.query || queryText,
          response: chatResponse.response || 'No response received',
          generatedSql: chatResponse.generatedSql,
          data: chatResponse.data,
          isSuccessful: chatResponse.isSuccessful !== false
        };
        
        console.log('Adding response:', newResponse);
        setResponses(prev => [...prev, newResponse]);
        // setQuery('');
      } else {
        console.log('Invalid response format:', response.data);
        setResponses(prev => [...prev, {
          userQuery: queryText,
          response: 'Unexpected response format',
          isSuccessful: false
        }]);
      }
    } catch (error) {
      console.error('Error details:', {
        message: error.message,
        response: error.response?.data,
        status: error.response?.status
      });
      
      const errorMessage = error.response?.data?.message || error.message || 'Error processing query';
      setResponses(prev => [...prev, {
        userQuery: query,
        response: `Error: ${errorMessage}`,
        isSuccessful: false
      }]);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="chatbot-container" style={{ maxWidth: '1000px', margin: '0 auto', padding: '20px' }}>
      <h1>Financial Data Chatbot</h1>

      {!isLoggedIn ? (
        <div style={{ 
          border: '1px solid #ddd', 
          borderRadius: '8px', 
          padding: '30px', 
          maxWidth: '400px', 
          margin: '0 auto',
          backgroundColor: '#f9f9f9'
        }}>
          <h2>Login</h2>
          {loginError && (
            <div style={{ 
              color: '#dc3545', 
              marginBottom: '15px', 
              padding: '10px', 
              backgroundColor: '#fff0f0',
              borderRadius: '4px'
            }}>
              {loginError}
            </div>
          )}
          <form onSubmit={handleLogin}>
            <div style={{ marginBottom: '15px' }}>
              <label style={{ display: 'block', marginBottom: '5px', fontWeight: 'bold' }}>
                Username:
              </label>
              <input
                type="text"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                placeholder="Enter your username"
                style={{ 
                  width: '100%', 
                  padding: '10px', 
                  border: '1px solid #ccc', 
                  borderRadius: '4px',
                  fontSize: '14px',
                  boxSizing: 'border-box'
                }}
                disabled={loginLoading}
                required
              />
            </div>
            <div style={{ marginBottom: '20px' }}>
              <label style={{ display: 'block', marginBottom: '5px', fontWeight: 'bold' }}>
                Password:
              </label>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Enter your password"
                style={{ 
                  width: '100%', 
                  padding: '10px', 
                  border: '1px solid #ccc', 
                  borderRadius: '4px',
                  fontSize: '14px',
                  boxSizing: 'border-box'
                }}
                disabled={loginLoading}
                required
              />
            </div>
            <button 
              type="submit" 
              disabled={loginLoading}
              style={{ 
                width: '100%',
                padding: '10px 20px', 
                cursor: 'pointer',
                backgroundColor: '#007bff',
                color: 'white',
                border: 'none',
                borderRadius: '4px',
                fontSize: '16px',
                fontWeight: 'bold',
                opacity: loginLoading ? 0.6 : 1
              }}
            >
              {loginLoading ? 'Logging in...' : 'Login'}
            </button>
          </form>
        </div>
      ) : (
        <>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
            <span style={{ color: '#666' }}>Logged in</span>
            <button 
              onClick={handleLogout}
              style={{ 
                padding: '8px 16px', 
                cursor: 'pointer',
                backgroundColor: '#dc3545',
                color: 'white',
                border: 'none',
                borderRadius: '4px',
                fontSize: '14px'
              }}
            >
              Logout
            </button>
          </div>

          <div style={{ marginBottom: '20px' }}>
            <label>Select Company (Optional): </label>
            <select 
              value={selectedCompany || ''} 
              onChange={(e) => setSelectedCompany(e.target.value ? parseInt(e.target.value) : null)}
              style={{ padding: '8px', marginLeft: '10px' }}
            >
              <option value="">All Companies</option>
              {companies.map(company => (
                <option key={company.id} value={company.id}>{company.name}</option>
              ))}
            </select>
          </div>

          <form onSubmit={handleSendQuery} style={{ marginBottom: '20px', display: 'flex', gap: '10px' }}>
            <input
              type="text"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Ask about turnover, quarterly data, companies, etc..."
              style={{ flex: 1, padding: '10px', fontSize: '16px', borderRadius: '4px', border: '1px solid #ccc' }}
              disabled={loading}
            />
            <button 
              type="submit" 
              disabled={loading} 
              style={{ 
                padding: '10px 20px', 
                cursor: 'pointer',
                backgroundColor: '#007bff',
                color: 'white',
                border: 'none',
                borderRadius: '4px',
                fontSize: '16px'
              }}
            >
              {loading ? 'Loading...' : 'Send'}
            </button>
          </form>

          <div className="responses">
            {responses.map((resp, idx) => (
              <div key={idx} style={{
                border: '1px solid #ddd',
                borderRadius: '5px',
                padding: '15px',
                marginBottom: '15px',
                backgroundColor: resp.isSuccessful ? '#f0f8ff' : '#fff0f0'
              }}>
                <h3>Query: {resp.userQuery}</h3>
                <p><strong>Response:</strong> {resp.response}</p>
                {resp.generatedSql && (
                  <details>
                    <summary style={{ cursor: 'pointer', color: '#007bff' }}>SQL Query</summary>
                    <pre style={{ backgroundColor: '#f5f5f5', padding: '10px', overflow: 'auto', borderRadius: '4px' }}>
                      {resp.generatedSql}
                    </pre>
                  </details>
                )}
                {resp.data && (
                  <details>
                    <summary style={{ cursor: 'pointer', color: '#007bff' }}>Data</summary>
                    <pre style={{ backgroundColor: '#f5f5f5', padding: '10px', overflow: 'auto', borderRadius: '4px' }}>
                      {JSON.stringify(resp.data, null, 2)}
                    </pre>
                  </details>
                )}
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  );
};

export default ChatbotComponent;