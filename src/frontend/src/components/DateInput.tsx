import { InputHTMLAttributes } from 'react';
import { registerLocale } from 'react-datepicker';
import DatePicker from 'react-datepicker';
import moment from 'moment';

import 'react-datepicker/dist/react-datepicker.css';
import * as fns from 'date-fns/locale';
registerLocale('sk', fns.sk);

type Props = {
    onDateChange: (date: string) => void;
} & InputHTMLAttributes<HTMLInputElement>;

export default function DateInput(props: Props) {
    const value = typeof props.value == 'string' ? props.value : '';
    const date = value ? moment(value, 'D.M.YYYY').toDate() : null;

    return (
        <DatePicker
            className="govuk-input"
            selected={date}
            dateFormat={['d. M. yyyy', 'd.M.yyyy']}
            strictParsing={true}
            onChange={(v) => props.onDateChange(v instanceof Date ? moment(v).format('D.M.YYYY') : '')}
            locale={fns.sk}
        />
    );
}
