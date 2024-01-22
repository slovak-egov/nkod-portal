import { useState } from 'react';
import Button from './Button';

export default function AlertPublisher2() {
    const [closed, setClosed] = useState(false);

    return (
        <>
            {closed ? null : (
                <div style={{ position: 'fixed', inset: 0, background: 'rgba(0, 0, 0, 0.6)', zIndex: 100 }}>
                    <div
                        style={{
                            position: 'absolute',
                            top: '50%',
                            left: '50%',
                            transform: 'translate(-50%, -50%)',
                            background: 'white',
                            padding: '1em',
                            border: '2px solid red',
                            maxWidth: '20em',
                            textAlign: 'center'
                        }}
                    >
                        <p className="govuk-body">
                            Datasety vložené do 5.2.2024 budú vymazané! Pre vkladanie údajov prosím používatejte stále pôvodný portál{' '}
                            <a href="https://data.gov.sk">data.gov.sk</a>, až jeho do odstávky.
                        </p>

                        <Button style={{ margin: 0 }} onClick={() => setClosed(true)}>
                            Beriem na vedomie
                        </Button>
                    </div>
                </div>
            )}
        </>
    );
}
