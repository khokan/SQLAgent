import { useState, useEffect } from 'react';
import axios from 'axios';
import {
  LineChart,
  Line,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell
} from 'recharts';

const API_BASE_URL = 'https://localhost:5001/api';

const ChatbotComponent = () => {
  const [query, setQuery] = useState('');
  const [responses, setResponses] = useState([]);
  const [loading, setLoading] = useState(false);
  const [summarizing, setSummarizing] = useState(false);
  const [companies, setCompanies] = useState([]);
  const [selectedCompany, setSelectedCompany] = useState(null);
  const [token, setToken] = useState(localStorage.getItem('token'));
  const [isLoggedIn, setIsLoggedIn] = useState(!!localStorage.getItem('token'));
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [loginLoading, setLoginLoading] = useState(false);
  const [loginError, setLoginError] = useState('');
  const [selectedVisualization, setSelectedVisualization] = useState({});

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

  // Determine visualization type based on data structure
  const detectVisualizationType = (data) => {
    if (!data || typeof data !== 'object') return 'text';
    
    if (Array.isArray(data)) {
      if (data.length === 0) return 'empty';
      
      // Check if it's a single row of numeric data (good for KPI cards)
      if (data.length === 1 && typeof data[0] === 'object') {
        const values = Object.values(data[0]);
        const numericCount = values.filter(v => typeof v === 'number').length;
        if (numericCount >= Object.keys(data[0]).length * 0.7) {
          return 'kpi';
        }
      }
      
      // Check if it's time series data (good for charts)
      const hasDateField = data.some(row => 
        Object.keys(row).some(key => 
          key.toLowerCase().includes('date') || 
          key.toLowerCase().includes('month') ||
          key.toLowerCase().includes('quarter') ||
          key.toLowerCase().includes('year')
        )
      );
      
      if (hasDateField) return 'chart';
      
      // Default to table for multiple rows
      return 'table';
    }
    
    return 'text';
  };

  // Generate business summary using LLM
  const generateSummary = async (userQuery, sqlData, dataType) => {
    try {
      setSummarizing(true);
      console.log('Generating summary for data type:', dataType);
      
      const summaryResponse = await axios.post(
        `${API_BASE_URL}/chatbot/summarize`,
        {
          userQuery: userQuery,
          data: sqlData,
          dataType: dataType
        },
        {
          headers: { Authorization: `Bearer ${token}` }
        }
      );

      return summaryResponse.data?.data?.summary || 'Summary not available';
    } catch (error) {
      console.error('Error generating summary:', error);
      return 'Unable to generate summary';
    } finally {
      setSummarizing(false);
    }
  };

  // Render KPI Cards
  const renderKPICards = (data) => {
    if (!Array.isArray(data) || data.length === 0) return null;
    
    const row = data[0];
    return (
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '15px', marginTop: '15px' }}>
        {Object.entries(row).map(([key, value]) => (
          <div key={key} style={{
            backgroundColor: '#f8f9fa',
            border: '1px solid #dee2e6',
            borderRadius: '8px',
            padding: '15px',
            textAlign: 'center'
          }}>
            <p style={{ margin: '0 0 10px 0', color: '#666', fontSize: '14px', fontWeight: '500' }}>
              {key}
            </p>
            <p style={{ margin: '0', fontSize: '24px', fontWeight: 'bold', color: '#007bff' }}>
              {typeof value === 'number' ? value.toLocaleString() : value}
            </p>
          </div>
        ))}
      </div>
    );
  };

  // Render Chart with Recharts
  const renderChart = (data) => {
    if (!Array.isArray(data) || data.length === 0) return null;
    
    // Detect numeric columns for Y-axis
    const numericKeys = Object.keys(data[0]).filter(key => 
      typeof data[0][key] === 'number'
    );
    
    const dateKeys = Object.keys(data[0]).filter(key =>
      key.toLowerCase().includes('date') || 
      key.toLowerCase().includes('month') ||
      key.toLowerCase().includes('quarter') ||
      key.toLowerCase().includes('year')
    );
    
    const xAxisKey = dateKeys.length > 0 ? dateKeys[0] : Object.keys(data[0])[0];
    const yAxisKey = numericKeys.length > 0 ? numericKeys[0] : null;
    
    if (!yAxisKey) return null;
    
    // Determine chart type
    const chartType = data.length === 1 ? 'pie' : (dateKeys.length > 0 ? 'line' : 'bar');
    
    const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884D8', '#82CA9D', '#FFC658', '#FF7C7C'];
    
    return (
      <div style={{ marginTop: '15px', width: '100%' }}>
        <p style={{ fontSize: '12px', color: '#666', marginBottom: '10px' }}>
          📊 {chartType.toUpperCase()} Chart - {data.length} records
        </p>
        
        {chartType === 'line' && (
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={data} margin={{ top: 5, right: 30, left: 0, bottom: 5 }}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey={xAxisKey} angle={-45} textAnchor="end" height={80} />
              <YAxis />
              <Tooltip />
              <Legend />
              {numericKeys.map((key, idx) => (
                <Line 
                  key={key}
                  type="monotone" 
                  dataKey={key} 
                  stroke={COLORS[idx % COLORS.length]} 
                  strokeWidth={2}
                  dot={{ fill: COLORS[idx % COLORS.length], r: 4 }}
                />
              ))}
            </LineChart>
          </ResponsiveContainer>
        )}
        
        {chartType === 'bar' && (
          <ResponsiveContainer width="100%" height={300}>
            <BarChart data={data} margin={{ top: 5, right: 30, left: 0, bottom: 5 }}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey={xAxisKey} angle={-45} textAnchor="end" height={80} />
              <YAxis />
              <Tooltip />
              <Legend />
              {numericKeys.map((key, idx) => (
                <Bar 
                  key={key}
                  dataKey={key} 
                  fill={COLORS[idx % COLORS.length]}
                />
              ))}
            </BarChart>
          </ResponsiveContainer>
        )}
        
        {chartType === 'pie' && numericKeys.length > 0 && (
          <ResponsiveContainer width="100%" height={300}>
            <PieChart>
              <Pie
                data={data}
                dataKey={numericKeys[0]}
                nameKey={xAxisKey}
                cx="50%"
                cy="50%"
                outerRadius={80}
                label
              >
                {data.map((entry, index) => (
                  <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                ))}
              </Pie>
              <Tooltip />
            </PieChart>
          </ResponsiveContainer>
        )}
      </div>
    );
  };

  // Render specific chart type on demand
  const renderSpecificChart = (data, chartType) => {
    if (!Array.isArray(data) || data.length === 0) return null;
    
    const numericKeys = Object.keys(data[0]).filter(key => 
      typeof data[0][key] === 'number'
    );
    
    const dateKeys = Object.keys(data[0]).filter(key =>
      key.toLowerCase().includes('date') || 
      key.toLowerCase().includes('month') ||
      key.toLowerCase().includes('quarter') ||
      key.toLowerCase().includes('year')
    );
    
    const xAxisKey = dateKeys.length > 0 ? dateKeys[0] : Object.keys(data[0])[0];
    const yAxisKey = numericKeys.length > 0 ? numericKeys[0] : null;
    
    if (!yAxisKey && chartType !== 'kpi') return null;
    
    const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884D8', '#82CA9D', '#FFC658', '#FF7C7C'];
    
    return (
      <div style={{ marginTop: '15px', width: '100%' }}>
        {chartType === 'line' && (
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={data} margin={{ top: 5, right: 30, left: 0, bottom: 5 }}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey={xAxisKey} angle={-45} textAnchor="end" height={80} />
              <YAxis />
              <Tooltip />
              <Legend />
              {numericKeys.map((key, idx) => (
                <Line 
                  key={key}
                  type="monotone" 
                  dataKey={key} 
                  stroke={COLORS[idx % COLORS.length]} 
                  strokeWidth={2}
                  dot={{ fill: COLORS[idx % COLORS.length], r: 4 }}
                />
              ))}
            </LineChart>
          </ResponsiveContainer>
        )}
        
        {chartType === 'bar' && (
          <ResponsiveContainer width="100%" height={300}>
            <BarChart data={data} margin={{ top: 5, right: 30, left: 0, bottom: 5 }}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey={xAxisKey} angle={-45} textAnchor="end" height={80} />
              <YAxis />
              <Tooltip />
              <Legend />
              {numericKeys.map((key, idx) => (
                <Bar 
                  key={key}
                  dataKey={key} 
                  fill={COLORS[idx % COLORS.length]}
                />
              ))}
            </BarChart>
          </ResponsiveContainer>
        )}
        
        {chartType === 'pie' && (
          <ResponsiveContainer width="100%" height={300}>
            <PieChart>
              <Pie
                data={data}
                dataKey={yAxisKey}
                nameKey={xAxisKey}
                cx="50%"
                cy="50%"
                outerRadius={80}
                label
              >
                {data.map((entry, index) => (
                  <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                ))}
              </Pie>
              <Tooltip />
            </PieChart>
          </ResponsiveContainer>
        )}
        
        {chartType === 'kpi' && renderKPICards(data)}
      </div>
    );
  };

  // Visualization selector component
  const renderVisualizationSelector = (respIdx, data) => {
    if (!data) return null;
    
    return (
      <div style={{ marginBottom: '12px', display: 'flex', gap: '8px', flexWrap: 'wrap', alignItems: 'center' }}>
        <span style={{ fontSize: '12px', fontWeight: '500', color: '#333' }}>View as:</span>
        <button
          onClick={() => setSelectedVisualization(prev => ({ ...prev, [respIdx]: 'auto' }))}
          style={{
            padding: '6px 12px',
            backgroundColor: (selectedVisualization[respIdx] === 'auto' || !selectedVisualization[respIdx]) ? '#0066cc' : '#f0f0f0',
            color: (selectedVisualization[respIdx] === 'auto' || !selectedVisualization[respIdx]) ? 'white' : '#333',
            border: 'none',
            borderRadius: '4px',
            cursor: 'pointer',
            fontSize: '12px',
            fontWeight: '500'
          }}
        >
          🔄 Auto
        </button>
        <button
          onClick={() => setSelectedVisualization(prev => ({ ...prev, [respIdx]: 'kpi' }))}
          style={{
            padding: '6px 12px',
            backgroundColor: selectedVisualization[respIdx] === 'kpi' ? '#0066cc' : '#f0f0f0',
            color: selectedVisualization[respIdx] === 'kpi' ? 'white' : '#333',
            border: 'none',
            borderRadius: '4px',
            cursor: 'pointer',
            fontSize: '12px',
            fontWeight: '500'
          }}
        >
          📊 KPI Cards
        </button>
        <button
          onClick={() => setSelectedVisualization(prev => ({ ...prev, [respIdx]: 'line' }))}
          style={{
            padding: '6px 12px',
            backgroundColor: selectedVisualization[respIdx] === 'line' ? '#0066cc' : '#f0f0f0',
            color: selectedVisualization[respIdx] === 'line' ? 'white' : '#333',
            border: 'none',
            borderRadius: '4px',
            cursor: 'pointer',
            fontSize: '12px',
            fontWeight: '500'
          }}
        >
          📈 Line
        </button>
        <button
          onClick={() => setSelectedVisualization(prev => ({ ...prev, [respIdx]: 'bar' }))}
          style={{
            padding: '6px 12px',
            backgroundColor: selectedVisualization[respIdx] === 'bar' ? '#0066cc' : '#f0f0f0',
            color: selectedVisualization[respIdx] === 'bar' ? 'white' : '#333',
            border: 'none',
            borderRadius: '4px',
            cursor: 'pointer',
            fontSize: '12px',
            fontWeight: '500'
          }}
        >
          📊 Bar
        </button>
        <button
          onClick={() => setSelectedVisualization(prev => ({ ...prev, [respIdx]: 'pie' }))}
          style={{
            padding: '6px 12px',
            backgroundColor: selectedVisualization[respIdx] === 'pie' ? '#0066cc' : '#f0f0f0',
            color: selectedVisualization[respIdx] === 'pie' ? 'white' : '#333',
            border: 'none',
            borderRadius: '4px',
            cursor: 'pointer',
            fontSize: '12px',
            fontWeight: '500'
          }}
        >
          🥧 Pie
        </button>
        <button
          onClick={() => setSelectedVisualization(prev => ({ ...prev, [respIdx]: 'table' }))}
          style={{
            padding: '6px 12px',
            backgroundColor: selectedVisualization[respIdx] === 'table' ? '#0066cc' : '#f0f0f0',
            color: selectedVisualization[respIdx] === 'table' ? 'white' : '#333',
            border: 'none',
            borderRadius: '4px',
            cursor: 'pointer',
            fontSize: '12px',
            fontWeight: '500'
          }}
        >
          📋 Table
        </button>
      </div>
    );
  };

  // Render Table
  const renderTable = (data) => {
    if (!Array.isArray(data) || data.length === 0) return null;
    
    const columns = Object.keys(data[0]);
    
    return (
      <div style={{ overflowX: 'auto', marginTop: '15px' }}>
        <table style={{
          width: '100%',
          borderCollapse: 'collapse',
          fontSize: '14px'
        }}>
          <thead>
            <tr style={{ backgroundColor: '#f8f9fa', borderBottom: '2px solid #dee2e6' }}>
              {columns.map(col => (
                <th key={col} style={{
                  padding: '12px',
                  textAlign: 'left',
                  fontWeight: 'bold',
                  color: '#333'
                }}>
                  {col}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {data.map((row, idx) => (
              <tr key={idx} style={{ 
                borderBottom: '1px solid #dee2e6',
                backgroundColor: idx % 2 === 0 ? '#fff' : '#f9f9f9'
              }}>
                {columns.map(col => (
                  <td key={`${idx}-${col}`} style={{ padding: '12px' }}>
                    {typeof row[col] === 'number' ? row[col].toLocaleString() : row[col]}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    );
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
        
        // Detect visualization type based on data
        const dataType = detectVisualizationType(chatResponse.data);
        console.log('Detected data type:', dataType);
        
        // Generate business summary with LLM
        let summary = chatResponse.response;
        if (chatResponse.data && dataType !== 'empty') {
          summary = await generateSummary(queryText, chatResponse.data, dataType);
        }
        
        const newResponse = {
          userQuery: chatResponse.query || queryText,
          response: summary,
          generatedSql: chatResponse.generatedSql,
          data: chatResponse.data,
          dataType: dataType,
          isSuccessful: chatResponse.isSuccessful !== false
        };
        
        console.log('Adding response:', newResponse);
        setResponses(prev => [...prev, newResponse]);
      } else {
        console.log('Invalid response format:', response.data);
        setResponses(prev => [...prev, {
          userQuery: queryText,
          response: 'Unexpected response format',
          isSuccessful: false,
          dataType: 'empty'
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
        isSuccessful: false,
        dataType: 'empty'
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
                <h3 style={{ marginTop: '0', color: '#333' }}>📝 Query: {resp.userQuery}</h3>
                
                {summarizing && (
                  <div style={{
                    backgroundColor: '#e7f3ff',
                    border: '1px solid #0066cc',
                    borderRadius: '4px',
                    padding: '12px',
                    marginBottom: '15px',
                    color: '#0066cc'
                  }}>
                    ⏳ Generating AI-powered business summary...
                  </div>
                )}
                
                {/* AI Summary Card */}
                <div style={{
                  backgroundColor: resp.isSuccessful ? '#e7f3ff' : '#ffe7e7',
                  border: `2px solid ${resp.isSuccessful ? '#0066cc' : '#cc0000'}`,
                  borderRadius: '8px',
                  padding: '15px',
                  marginBottom: '20px'
                }}>
                  <h4 style={{ margin: '0 0 10px 0', color: resp.isSuccessful ? '#0066cc' : '#cc0000' }}>
                    🤖 AI Summary & Insights
                  </h4>
                  <p style={{ margin: '0', color: '#333', lineHeight: '1.6', whiteSpace: 'pre-wrap' }}>
                    {resp.response}
                  </p>
                </div>
                
                {/* Auto-rendered visualization based on data type */}
                {resp.data && resp.isSuccessful && (
                  <div style={{ marginTop: '20px', borderTop: '1px solid #ddd', paddingTop: '15px' }}>
                    <p style={{ fontWeight: 'bold', color: '#333', marginBottom: '10px', marginTop: '0' }}>
                      📊 Data Visualization
                    </p>
                    
                    {/* Visualization Type Selector */}
                    {renderVisualizationSelector(idx, resp.data, resp.dataType)}
                    
                    {/* Render based on selection */}
                    {(!selectedVisualization[idx] || selectedVisualization[idx] === 'auto') && (
                      <>
                        {resp.dataType === 'kpi' && renderKPICards(resp.data)}
                        {resp.dataType === 'chart' && renderChart(resp.data)}
                        {resp.dataType === 'table' && renderTable(resp.data)}
                        {resp.dataType === 'empty' && (
                          <p style={{ color: '#999', fontStyle: 'italic' }}>No data to visualize</p>
                        )}
                      </>
                    )}
                    
                    {selectedVisualization[idx] === 'kpi' && renderKPICards(resp.data)}
                    {selectedVisualization[idx] === 'line' && renderSpecificChart(resp.data, 'line')}
                    {selectedVisualization[idx] === 'bar' && renderSpecificChart(resp.data, 'bar')}
                    {selectedVisualization[idx] === 'pie' && renderSpecificChart(resp.data, 'pie')}
                    {selectedVisualization[idx] === 'table' && renderTable(resp.data)}
                  </div>
                )}
                
                {resp.generatedSql && (
                  <details style={{ marginTop: '15px' }}>
                    <summary style={{ cursor: 'pointer', color: '#007bff', fontWeight: '500', userSelect: 'none' }}>
                      🔍 View SQL Query
                    </summary>
                    <pre style={{ 
                      backgroundColor: '#f5f5f5', 
                      padding: '12px', 
                      overflow: 'auto', 
                      borderRadius: '4px', 
                      marginTop: '10px',
                      fontSize: '12px',
                      border: '1px solid #ddd'
                    }}>
                      {resp.generatedSql}
                    </pre>
                  </details>
                )}
                
                {resp.data && (
                  <details style={{ marginTop: '10px' }}>
                    <summary style={{ cursor: 'pointer', color: '#007bff', fontWeight: '500', userSelect: 'none' }}>
                      📋 View Raw Data ({Array.isArray(resp.data) ? resp.data.length : 1} record(s))
                    </summary>
                    <pre style={{ 
                      backgroundColor: '#f5f5f5', 
                      padding: '12px', 
                      overflow: 'auto', 
                      borderRadius: '4px', 
                      marginTop: '10px',
                      fontSize: '12px',
                      border: '1px solid #ddd',
                      maxHeight: '300px'
                    }}>
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